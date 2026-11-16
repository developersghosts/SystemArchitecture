namespace ServicesA.Application.Features.Identity.Common;

public static class IdentityValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(100);

    public static IRuleBuilderOptions<T, string> ValidUserName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MinimumLength(3).MaximumLength(256);

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().EmailAddress().MaximumLength(256);

    /// <summary>Length only; character rules come from the Identity password policy.</summary>
    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MinimumLength(8).MaximumLength(128);
}
