namespace ServicesA.Domain.Entities.Identity;

/// <summary>Role names used by the Identity endpoints. Seeded at startup.</summary>
public static class IdentityRoles
{
    public const string Administrator = "Administrator";
    public const string RegisterUser = "RegisterUser";
    public const string EditUser = "EditUser";
    public const string GetAllUsers = "GetAllUsers";
    public const string CreateRole = "CreateRole";
    public const string GetAllRolesList = "GetAllRolesList";
    public const string CreateUserRole = "CreateUserRole";

    public static readonly IReadOnlyList<(string Name, string Title)> All =
    [
        (Administrator, "Administrator"),
        (RegisterUser, "Register users"),
        (EditUser, "Edit users"),
        (GetAllUsers, "View users"),
        (CreateRole, "Create roles"),
        (GetAllRolesList, "View roles"),
        (CreateUserRole, "Assign roles to users")
    ];
}
