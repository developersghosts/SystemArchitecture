using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword, string ConfirmPassword)
    : ICommand<bool>;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).ValidPassword();
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword);
    }
}

public sealed class ResetPasswordHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    EmailTemplates emailTemplates,
    IStringLocalizerService localizer) : ICommandHandler<ResetPasswordCommand, bool>
{
    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        // Same answer for an unknown email and a bad token.
        if (user is null)
            throw new BadRequestException(localizer.Get(ResourceKeys.InvalidResetToken));

        // The client may send the token still URL-encoded from the link.
        var token = Uri.UnescapeDataString(request.Token);

        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword); // rotates the security stamp
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                throw new BadRequestException(localizer.Get(ResourceKeys.InvalidResetToken));
            result.EnsureSucceeded();
        }

        user.EndSession();
        (await userManager.UpdateAsync(user)).EnsureSucceeded();

        var body = await emailTemplates.BuildAsync(
            localizer.Get(ResourceKeys.PasswordChangedTitle),
            localizer.Get(ResourceKeys.PasswordChangedBody),
            highlight: user.UserName);
        await emailService.SendEmailAsync(user.Email!, localizer.Get(ResourceKeys.PasswordChangedTitle), body, cancellationToken);

        return true;
    }
}
