/** Reads the JWT payload (no signature check: the API does that). Returns null for a malformed token. */
export function decodeJwt(token: string): Record<string, unknown> | null {
  try {
    const payload = token.split('.')[1];
    if (!payload) return null;
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(payload.length / 4) * 4, '=');
    const json = decodeURIComponent(
      Array.from(atob(base64), (c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0')).join('')
    );
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

/** The "exp" claim in ms, or null. */
export function jwtExpiry(token: string): number | null {
  const exp = decodeJwt(token)?.['exp'];
  return typeof exp === 'number' ? exp * 1000 : null;
}
