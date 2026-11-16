namespace ServicesA.Application.Features.Identity.Common;

public sealed record LoginResponse(bool Success, string Message);

public sealed record AuthResponse(
    Guid Id,
    string UserName,
    string FullName,
    string Email,
    DateOnly? DateOfBirth,
    IReadOnlyList<string> Roles,
    string Token,
    DateTime ExpiresOnUtc);

public sealed record UserListItemDto(
    Guid Id,
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastActiveAtUtc);

public sealed record RoleListDto(Guid Id, string Name, string Title, string? Description);

public sealed record UserRoleDto(Guid UserId, string UserName, string FullName, Guid RoleId, string RoleName, string RoleTitle);

public sealed record OtpDto(string FullName, string? Email, string? OtpCode, DateTime? OtpExpiresAtUtc);
