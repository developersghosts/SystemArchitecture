using ServicesA.Application.Features.Identity.UserGroups;
using ServicesA.Application.Features.Identity.UserGroups.Commands;
using ServicesA.Application.Features.Identity.UserGroups.Queries;

namespace ServicesA.API.Endpoints.Identity.UserGroups;

/// <summary>User groups: a named set of roles that can be given to many users at once.</summary>
public class UserGroupsApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(UrlTemplates.UserGroupsRoute).WithTags("UserGroup");

        // Reading: Administrator or GetAllRolesList (same as the roles list)
        group.MapGet("/getAllGroups",
                async ([AsParameters] PaginationParams paging, ISender sender, HttpContext context, CancellationToken token) =>
                {
                    var groups = await sender.Send(new GetUserGroupsQuery(paging), token);
                    context.Response.AddPagination(groups);
                    return Results.Ok(new { items = groups, totalCount = groups.TotalCount, totalPages = groups.TotalPages });
                })
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.GetAllRolesList, IdentityRoles.Administrator));

        group.MapGet("/{id:guid}",
                async (Guid id, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(new GetUserGroupQuery(id), token)))
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.GetAllRolesList, IdentityRoles.Administrator))
            .Produces<UserGroupDetailsDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/user/{userId:guid}",
                async (Guid userId, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(new GetGroupsOfUserQuery(userId), token)))
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.GetAllRolesList, IdentityRoles.Administrator))
            .Produces<List<UserGroupListItemDto>>();

        // Changing groups gives or removes permissions: Administrator only
        group.MapPost("/createGroup",
                async (CreateUserGroupCommand command, ISender sender, CancellationToken token) =>
                {
                    var created = await sender.Send(command, token);
                    return Results.Created($"{UrlTemplates.UserGroupsRoute}/{created.Id}", created);
                })
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .Produces<UserGroupDetailsDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/updateGroup",
                async (UpdateUserGroupCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .Produces<UserGroupDetailsDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}",
                async (Guid id, ISender sender, CancellationToken token) =>
                {
                    await sender.Send(new DeleteUserGroupCommand(id), token);
                    return Results.NoContent();
                })
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/setUserGroups",
                async (SetUserGroupsCommand command, ISender sender, CancellationToken token) =>
                    Results.Ok(await sender.Send(command, token)))
            .RequireAuthorization(p => p.RequireRole(IdentityRoles.Administrator))
            .Produces<List<UserGroupListItemDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
