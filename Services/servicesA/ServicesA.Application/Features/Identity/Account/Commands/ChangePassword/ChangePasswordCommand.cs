using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Commands.ChangePassword;

/// <summary>The user always comes from the JWT, never from the body.</summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<bool>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).ValidPassword().NotEqual(x => x.CurrentPassword);
    }
}

public sealed class ChangePasswordHandler(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser,
    IEmailService emailService,
    EmailTemplates emailTemplates,
    IStringLocalizerService localizer) : ICommandHandler<ChangePasswordCommand, bool>
{
    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            throw new BadRequestException(localizer.Get(ResourceKeys.CurrentUserUnknown));

        var user = await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException(localizer.Get(ResourceKeys.UserNotFound));

        // Verifies the current password and rotates the security stamp, so every existing token is rejected.
        (await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword)).EnsureSucceeded();

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
