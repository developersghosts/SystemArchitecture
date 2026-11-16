using System.Security.Cryptography;
using ServicesA.Application.Features.Identity.Account.Commands.Login;
using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Commands.ResendOtp;

/// <summary>
/// Sends a new code, but only to a user who already passed the password step (has a pending code).
/// Always returns true so the response does not reveal whether the user exists.
/// </summary>
public sealed record ResendOtpCommand(string UserName) : ICommand<bool>;

public sealed class ResendOtpValidator : AbstractValidator<ResendOtpCommand>
{
    public ResendOtpValidator() => RuleFor(x => x.UserName).NotEmpty();
}

public sealed class ResendOtpHandler(
    UserManager<ApplicationUser> userManager,
    IAppDbContext db,
    IEmailService emailService,
    EmailTemplates emailTemplates,
    IStringLocalizerService localizer,
    TimeProvider clock) : ICommandHandler<ResendOtpCommand, bool>
{
    public async Task<bool> Handle(ResendOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.UserName.Trim());
        if (user is null || !user.IsActive || user.OtpCode is null)
            return true;

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        user.IssueOtp(otp, clock.GetUtcNow().UtcDateTime.Add(LoginHandler.OtpLifetime));
        await db.SaveChangesAsync(cancellationToken);

        await LoginHandler.SendOtpAsync(emailService, emailTemplates, localizer, user, otp, cancellationToken);
        return true;
    }
}
