using System.Security.Cryptography;
using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Commands.Login;

/// <summary>Step 1 of login: checks user name + password and emails a one-time code.</summary>
public sealed record LoginCommand(string UserName, string Password, bool ForceLogin = false) : ICommand<LoginResponse>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserName).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginHandler(
    UserManager<ApplicationUser> userManager,
    IAppDbContext db,
    IEmailService emailService,
    EmailTemplates emailTemplates,
    IStringLocalizerService localizer,
    LoginHistoryRecorder history,
    TimeProvider clock) : ICommandHandler<LoginCommand, LoginResponse>
{
    public static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await LoginGuard.CheckCredentialsAsync(userManager, db, localizer, history,
            request.UserName, request.Password, cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;

        // Only one active session per user, unless the caller explicitly takes over the other session.
        if (user.HasActiveSession(now) && !request.ForceLogin)
            throw new BadRequestException(localizer.Get(ResourceKeys.User_Already_Logged_In), "USER_ALREADY_LOGGED_IN");

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        user.IssueOtp(otp, now.Add(OtpLifetime));
        await db.SaveChangesAsync(cancellationToken);

        await SendOtpAsync(emailService, emailTemplates, localizer, user, otp, cancellationToken);

        return new LoginResponse(true, localizer.Get(ResourceKeys.OTP_Sent));
    }

    internal static async Task SendOtpAsync(IEmailService emailService, EmailTemplates emailTemplates,
        IStringLocalizerService localizer, ApplicationUser user, string otp, CancellationToken cancellationToken)
    {
        var body = await emailTemplates.BuildAsync(
            localizer.Get(ResourceKeys.OtpEmailTitle),
            localizer.Get(ResourceKeys.OtpEmailBody),
            highlight: otp);
        await emailService.SendEmailAsync(user.Email!, localizer.Get(ResourceKeys.OtpEmailTitle), body, cancellationToken);
    }
}

/// <summary>Password check shared by login and verify-otp: lockout, failed-attempt counting and login history.</summary>
internal static class LoginGuard
{
    public static async Task<ApplicationUser> CheckCredentialsAsync(
        UserManager<ApplicationUser> userManager, IAppDbContext db, IStringLocalizerService localizer,
        LoginHistoryRecorder history, string userName, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(userName.Trim());

        // Same message whether the user exists or not, so user names can't be discovered.
        if (user is null)
            throw new BadRequestException(localizer.Get(ResourceKeys.Login_Information_Incorrect));

        if (await userManager.IsLockedOutAsync(user))
            throw new BadRequestException(localizer.Get(ResourceKeys.AccountLocked), "ACCOUNT_LOCKED");

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user); // locks the account after MaxFailedAccessAttempts
            history.Record(user.Id, succeeded: false);
            await db.SaveChangesAsync(cancellationToken);
            throw new BadRequestException(localizer.Get(ResourceKeys.Login_Information_Incorrect));
        }

        if (!user.IsActive)
            throw new BadRequestException(localizer.Get(ResourceKeys.UserNotActive), "USER_NOT_ACTIVE");

        await userManager.ResetAccessFailedCountAsync(user);
        return user;
    }
}
