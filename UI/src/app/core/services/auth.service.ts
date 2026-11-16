import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  LoginRequest,
  LoginResponse,
  PendingLogin,
  Session
} from '../models/auth.models';
import { jwtExpiry } from '../utils/jwt';
import { safeStorage } from '../utils/storage';

const STORAGE_KEY = 'sa.session';

/** The "your session is about to end" dialog opens this long before the token expires. */
export const SESSION_WARNING_MS = 5 * 60 * 1000;

/**
 * Two-step login against ServicesA:
 * 1) POST /login checks the password and emails a code, 2) POST /verify-otp returns the JWT.
 * The password is kept in memory only between the two steps, because verify-otp needs it again.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly api = `${environment.apiUrl}/api/Account`;

  private readonly _session = signal<Session | null>(restoreSession());
  private readonly _pending = signal<PendingLogin | null>(null);
  private readonly _expiringSoon = signal(false);
  private expiryTimer?: ReturnType<typeof setTimeout>;
  private warningTimer?: ReturnType<typeof setTimeout>;

  readonly session = this._session.asReadonly();
  readonly pendingLogin = this._pending.asReadonly();
  readonly user = computed(() => this._session()?.user ?? null);
  readonly roles = computed(() => this._session()?.user.roles ?? []);
  /** True during the last SESSION_WARNING_MS of the session (shows the "renew?" dialog). */
  readonly expiringSoon = this._expiringSoon.asReadonly();

  constructor() {
    // Warns 5 minutes before the token expires, and signs out when it expires.
    effect(() => {
      clearTimeout(this.expiryTimer);
      clearTimeout(this.warningTimer);
      this._expiringSoon.set(false);
      const session = this._session();
      if (!session) return;

      const remaining = session.expiresAt - Date.now();
      if (remaining > 2_147_000_000) return; // setTimeout's max delay is ~24.8 days
      this.expiryTimer = setTimeout(() => this.onExpired(), Math.max(0, remaining));
      this.warningTimer = setTimeout(() => this._expiringSoon.set(true), Math.max(0, remaining - SESSION_WARNING_MS));
    });

    // Keep every open tab in step: a renew, sign-in or sign-out in one tab applies to the others.
    window.addEventListener('storage', (event) => {
      if (event.key !== STORAGE_KEY) return;
      const session = restoreSession();
      if (session) this._session.set(session);
      else if (this._session()) this.endSession(false, /* fromOtherTab */ true);
    });
  }

  /** Asks the API for a new token for the same session (another full lifetime). */
  refreshSession(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.api}/refresh-session`, {}).pipe(tap((r) => this.startSession(r)));
  }

  private onExpired(): void {
    // Another tab may have renewed the session meanwhile.
    const latest = restoreSession();
    if (latest && latest.expiresAt > Date.now()) {
      this._session.set(latest);
      return;
    }
    this.endSession(true);
  }

  get token(): string | null {
    return this.isAuthenticated() ? this._session()!.token : null;
  }

  isAuthenticated(): boolean {
    const session = this._session();
    return !!session && session.expiresAt > Date.now();
  }

  hasAnyRole(roles: readonly string[]): boolean {
    const mine = this.roles();
    return roles.some((r) => mine.includes(r));
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.api}/login`, request).pipe(
      tap((response) =>
        this._pending.set({ userName: request.userName.trim(), password: request.password, message: response.message })
      )
    );
  }

  verifyOtp(otp: string): Observable<AuthResponse> {
    const pending = this._pending()!;
    return this.http
      .post<AuthResponse>(`${this.api}/verify-otp`, { userName: pending.userName, password: pending.password, otp })
      .pipe(
        tap((response) => {
          this.startSession(response);
          this._pending.set(null);
        })
      );
  }

  resendOtp(): Observable<boolean> {
    return this.http.post<boolean>(`${this.api}/resend-otp`, { userName: this._pending()!.userName });
  }

  /** Always succeeds (the API does not tell whether the email exists). */
  forgotPassword(email: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.api}/forgot-password`, { email });
  }

  resetPassword(email: string, token: string, newPassword: string, confirmPassword: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.api}/reset-password`, { email, token, newPassword, confirmPassword });
  }

  /** The API ends every session after a password change, so the caller must sign in again. */
  changePassword(currentPassword: string, newPassword: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.api}/change-password`, { currentPassword, newPassword });
  }

  cancelPendingLogin(): void {
    this._pending.set(null);
  }

  /** Ends the session on the server (so the token stops working) and locally. */
  logout(): void {
    if (!this.isAuthenticated()) {
      this.endSession(false);
      return;
    }
    this.http
      .post(`${this.api}/logout`, {})
      .pipe(finalize(() => this.endSession(false)))
      .subscribe({ error: () => undefined });
  }

  /**
   * Clears the local session; used on logout, 401 responses, token expiry and password change.
   * `true` / 'expired' shows "session ended" on the login page, 'passwordChanged' shows "sign in with the new password".
   */
  endSession(reason: boolean | 'expired' | 'passwordChanged', fromOtherTab = false): void {
    clearTimeout(this.expiryTimer);
    clearTimeout(this.warningTimer);
    this._expiringSoon.set(false);
    this._session.set(null);
    if (!fromOtherTab) safeStorage.remove(STORAGE_KEY);
    const queryParams =
      reason === 'passwordChanged' ? { passwordChanged: 1 } : reason ? { expired: 1 } : {};
    void this.router.navigate(['/auth/login'], { queryParams });
  }

  private startSession(response: AuthResponse): void {
    const { token, expiresOnUtc, ...user } = response;
    const expiresAt = jwtExpiry(token) ?? Date.parse(expiresOnUtc);
    const session: Session = { user, token, expiresAt };
    this._session.set(session);
    safeStorage.set(STORAGE_KEY, JSON.stringify(session));
  }
}

function restoreSession(): Session | null {
  const raw = safeStorage.get(STORAGE_KEY);
  if (!raw) return null;
  try {
    const session = JSON.parse(raw) as Session;
    if (!session?.token || !session.user || !(session.expiresAt > Date.now())) {
      safeStorage.remove(STORAGE_KEY);
      return null;
    }
    return session;
  } catch {
    safeStorage.remove(STORAGE_KEY);
    return null;
  }
}
