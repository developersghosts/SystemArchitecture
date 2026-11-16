using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Roles.Commands.CreateRole;

public sealed record CreateRoleItem(string Name, string Title, string? Description);

/// <summary>Creates several roles at once; roles that already exist are skipped. Returns the created titles.</summary>
public sealed record CreateRoleCommand(List<CreateRoleItem> Roles) : ICommand<List<string>>;

public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Roles).NotEmpty();
        RuleForEach(x => x.Roles).ChildRules(role =>
        {
            role.RuleFor(r => r.Name).NotEmpty().MinimumLength(3).MaximumLength(256).Matches("^[A-Za-z0-9_.-]+$");
            role.RuleFor(r => r.Title).NotEmpty().MinimumLength(3).MaximumLength(256);
            role.RuleFor(r => r.Description).MaximumLength(500);
        });
    }
}

public sealed class CreateRoleHandler(RoleManager<ApplicationRole> roleManager)
    : ICommandHandler<CreateRoleCommand, List<string>>
{
    public async Task<List<string>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var created = new List<string>();

        foreach (var item in request.Roles.DistinctBy(r => r.Name.Trim().ToUpperInvariant()))
        {
            if (await roleManager.RoleExistsAsync(item.Name.Trim()))
                continue;

            var role = ApplicationRole.Create(item.Name, item.Title, item.Description);
            (await roleManager.CreateAsync(role)).EnsureSucceeded();
            created.Add(role.Title);
        }

        return created;
    }
}
