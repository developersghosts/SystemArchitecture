using ServicesA.Application.Features.Identity.Account.Commands.Login;
using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Commands.VerifyOtp;

/// <summary>Step 2 of login: checks the emailed code, starts a new session and returns the JWT.</summary>
public sealed record VerifyOtpCommand(string UserName, string Password, string Otp) : ICommand<AuthResponse>;

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.UserName).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Otp).NotEmpty().Length(6).Matches("^[0-9]+$");
    }
}

public sealed class VerifyOtpHandler(
    UserManager<ApplicationUser> userManager,
    IAppDbContext db,
    IJwtTokenService jwt,
    IEmailService emailService,
    EmailTemplates emailTemplates,
    IStringLocalizerService localizer,
    LoginHistoryRecorder history,
    UserRoleResolver roleResolver,
    TimeProvider clock) : ICommandHandler<VerifyOtpCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await LoginGuard.CheckCredentialsAsync(userManager, db, localizer, history,
            request.UserName, request.Password, cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;

        if (!user.TryConsumeOtp(request.Otp, now))
        {
            history.Record(user.Id, succeeded: false);
            await db.SaveChangesAsync(cancellationToken); // keeps the failed-attempt counter
            throw new BadRequestException(localizer.Get(ResourceKeys.OTP_FAIL), "OTP_INVALID");
        }

        // A new session replaces any previous one, so older tokens stop working.
        var expiresAt = now.Add(jwt.Lifetime);
        var sessionId = user.StartSession(expiresAt, now);
        // Own roles + roles from the user's groups.
        var roles = await roleResolver.GetEffectiveRolesAsync(user, cancellationToken);
        var token = jwt.CreateToken(user, roles, sessionId, expiresAt);

        history.Record(user.Id, succeeded: true);
        await db.SaveChangesAsync(cancellationToken);

        var body = await emailTemplates.BuildAsync(
            localizer.Get(ResourceKeys.SuccessLoginTitle),
            localizer.Get(ResourceKeys.IFYouNotLoggedIn),
            highlight: user.UserName);
        await emailService.SendEmailAsync(user.Email!, localizer.Get(ResourceKeys.LoginSuccessful), body, cancellationToken);

        return new AuthResponse(user.Id, user.UserName!, user.FullName.ToString(), user.Email!,
            user.DateOfBirth, roles, token, expiresAt);
    }
}
