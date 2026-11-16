using ServicesA.Application.Features.Identity.Account.Commands.EditUser;

namespace ServicesA.API.Endpoints.Identity.Account.Command;

public class EditUserApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{UrlTemplates.AccountRoute}/editUser",
                async (EditUserCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .WithTags("Account")
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.EditUser, IdentityRoles.Administrator))
            .Produces<Guid>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
