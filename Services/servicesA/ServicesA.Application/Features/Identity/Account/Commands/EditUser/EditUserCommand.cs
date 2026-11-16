using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Commands.EditUser;

public sealed record EditUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    DateOnly? DateOfBirth,
    string? PhoneNumber,
    bool IsActive) : ICommand<Guid>;

public sealed class EditUserValidator : AbstractValidator<EditUserCommand>
{
    public EditUserValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).ValidName();
        RuleFor(x => x.LastName).ValidName();
        RuleFor(x => x.UserName).ValidUserName();
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
    }
}

public sealed class EditUserHandler(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser,
    IStringLocalizerService localizer) : ICommandHandler<EditUserCommand, Guid>
{
    public async Task<Guid> Handle(EditUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.Id.ToString())
                   ?? throw new NotFoundException(localizer.Get(ResourceKeys.UserNotFound));

        // Deactivating yourself would lock you out with no one left to undo it.
        if (!request.IsActive && currentUser.UserId == user.Id)
            throw new BadRequestException(localizer.Get(ResourceKeys.CannotDeactivateYourself), "CANNOT_DEACTIVATE_SELF");

        // UserManager normalizes, checks uniqueness and saves.
        if (!string.Equals(user.UserName, request.UserName.Trim(), StringComparison.Ordinal))
            (await userManager.SetUserNameAsync(user, request.UserName.Trim())).EnsureSucceeded();

        if (!string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            (await userManager.SetEmailAsync(user, request.Email.Trim())).EnsureSucceeded();

        user.UpdateProfile(request.FirstName, request.LastName, request.DateOfBirth, request.PhoneNumber);

        var wasActive = user.IsActive;
        if (request.IsActive) user.Activate();
        else user.Deactivate(); // also ends the session

        (await userManager.UpdateAsync(user)).EnsureSucceeded();

        // A deactivated user must not keep using an existing token.
        if (wasActive && !request.IsActive)
            await userManager.UpdateSecurityStampAsync(user);

        return user.Id;
    }
}
