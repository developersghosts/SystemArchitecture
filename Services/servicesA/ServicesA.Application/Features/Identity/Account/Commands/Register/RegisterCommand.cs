using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Commands.Register;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password,
    DateOnly? DateOfBirth,
    string? PhoneNumber) : ICommand<Guid>;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FirstName).ValidName();
        RuleFor(x => x.LastName).ValidName();
        RuleFor(x => x.UserName).ValidUserName();
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password).ValidPassword();
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
    }
}

public sealed class RegisterHandler(UserRegistrar registrar) : ICommandHandler<RegisterCommand, Guid>
{
    public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = await registrar.RegisterAsync(
            new NewUser(request.FirstName, request.LastName, request.UserName, request.Email, request.Password,
                request.DateOfBirth, request.PhoneNumber),
            roleIds: null, cancellationToken);

        return user.Id;
    }
}
