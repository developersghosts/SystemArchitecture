namespace ServicesA.Application.Features.Identity.Common;

public sealed record NewUser(
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password,
    DateOnly? DateOfBirth,
    string? PhoneNumber);

/// <summary>Shared by register and registerUserFull: creates the user, assigns roles and sends a welcome email.</summary>
public sealed class UserRegistrar(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IEmailService emailService,
    EmailTemplates emailTemplates,
    IStringLocalizerService localizer)
{
    public async Task<ApplicationUser> RegisterAsync(NewUser input, IReadOnlyCollection<Guid>? roleIds,
        CancellationToken cancellationToken)
    {
        var roleNames = await ResolveRoleNamesAsync(roleIds, cancellationToken);

        var user = ApplicationUser.Create(input.UserName, input.Email, input.FirstName, input.LastName,
            input.DateOfBirth, input.PhoneNumber);

        // Checks duplicate user name / email (RequireUniqueEmail) and the password policy.
        (await userManager.CreateAsync(user, input.Password)).EnsureSucceeded();

        if (roleNames.Count > 0)
            (await userManager.AddToRolesAsync(user, roleNames)).EnsureSucceeded();

        // The password is never emailed.
        var body = await emailTemplates.BuildAsync(
            $"{localizer.Get(ResourceKeys.WelcomeTitle)} {user.FullName}",
            localizer.Get(ResourceKeys.WelcomeBody),
            highlight: user.UserName);
        await emailService.SendEmailAsync(user.Email!, localizer.Get(ResourceKeys.WelcomeTitle), body, cancellationToken);

        return user;
    }

    public async Task<List<string>> ResolveRoleNamesAsync(IReadOnlyCollection<Guid>? roleIds,
        CancellationToken cancellationToken)
    {
        var ids = (roleIds ?? []).Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var names = await roleManager.Roles
            .Where(r => ids.Contains(r.Id))
            .Select(r => r.Name!)
            .ToListAsync(cancellationToken);

        if (names.Count != ids.Count)
            throw new BadRequestException(localizer.Get(ResourceKeys.RoleNotFound));

        return names;
    }
}
