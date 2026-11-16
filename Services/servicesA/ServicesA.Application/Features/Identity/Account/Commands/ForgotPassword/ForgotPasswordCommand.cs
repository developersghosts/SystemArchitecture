using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Commands.ForgotPassword;

/// <summary>Emails a reset link. Always returns true so it can't be used to discover registered emails.</summary>
public sealed record ForgotPasswordCommand(string Email) : ICommand<bool>;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator() => RuleFor(x => x.Email).ValidEmail();
}

public sealed class ForgotPasswordHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IEmailSettingsProvider emailSettings,
    EmailTemplates emailTemplates,
    IStringLocalizerService localizer) : ICommandHandler<ForgotPasswordCommand, bool>
{
    public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
            return true;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetPage = (await emailSettings.GetAsync(cancellationToken)).ResetPasswordUrl;
        var resetUrl = $"{resetPage}?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

        var body = await emailTemplates.BuildAsync(
            localizer.Get(ResourceKeys.ResetPasswordTitle),
            localizer.Get(ResourceKeys.ResetPasswordBody),
            buttonText: localizer.Get(ResourceKeys.ResetPasswordButton),
            buttonUrl: resetUrl);
        await emailService.SendEmailAsync(user.Email!, localizer.Get(ResourceKeys.ResetPasswordTitle), body, cancellationToken);

        return true;
    }
}
