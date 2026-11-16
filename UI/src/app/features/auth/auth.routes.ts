import { Routes } from '@angular/router';
import { otpGuard } from '../../core/guards/auth.guards';
import { AuthLayoutComponent } from './auth-layout/auth-layout.component';
import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { LoginComponent } from './login/login.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { VerifyOtpComponent } from './verify-otp/verify-otp.component';

/** Lazy loaded at /auth. */
export const AUTH_ROUTES: Routes = [
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'login' },
      { path: 'login', component: LoginComponent, title: 'login.title' },
      { path: 'verify-otp', component: VerifyOtpComponent, canActivate: [otpGuard], title: 'otp.title' },
      { path: 'forgot-password', component: ForgotPasswordComponent, title: 'forgot.title' },
      // Link from the reset email: ?email=...&token=... (EmailSettings:ResetPasswordUrl)
      { path: 'reset-password', component: ResetPasswordComponent, title: 'reset.title' }
    ]
  }
];
