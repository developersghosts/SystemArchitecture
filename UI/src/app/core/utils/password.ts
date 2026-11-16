import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Same rules as the Identity password policy (Infrastructure/ServiceInjection.cs): 8+ chars, upper, lower, digit, symbol. */
export const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,128}$/;

/** Group validator: `second` must equal `first`. Puts { mismatch: true } on the second control. */
export function matchValidator(first: string, second: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const a = group.get(first);
    const b = group.get(second);
    if (!a || !b) return null;
    const mismatch = !!b.value && a.value !== b.value;
    const errors = { ...(b.errors ?? {}) };
    if (mismatch) errors['mismatch'] = true;
    else delete errors['mismatch'];
    b.setErrors(Object.keys(errors).length ? errors : null);
    return null;
  };
}
