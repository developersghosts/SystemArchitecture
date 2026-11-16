using Microsoft.AspNetCore.Identity;
using ServicesA.Domain.Common;
using ServicesA.Domain.Entities.Identity.Events;

namespace ServicesA.Domain.Entities.Identity;

/// <summary>User aggregate root. Extends the ASP.NET Core Identity user (Guid key).</summary>
public class ApplicationUser : IdentityUser<Guid>, IHasDomainEvents, IAuditableEntity
{
    public const int MaxOtpAttempts = 5;

    private readonly List<IDomainEvent> _domainEvents = [];

    public FullName FullName { get; private set; } = null!;
    public DateOnly? DateOfBirth { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ModifiedAtUtc { get; private set; }
    public DateTime? LastActiveAtUtc { get; private set; }

    // One-time password sent by email as the second login step.
    public string? OtpCode { get; private set; }
    public DateTime? OtpExpiresAtUtc { get; private set; }
    public int OtpFailedAttempts { get; private set; }

    // The single active session. A JWT is only accepted while its "sid" claim matches this value.
    public Guid? ActiveSessionId { get; private set; }
    public DateTime? ActiveSessionExpiresAtUtc { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Required by EF Core.
    private ApplicationUser() { }

    public static ApplicationUser Create(string userName, string email, string firstName, string lastName,
        DateOnly? dateOfBirth = null, string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = userName.Trim(),
            Email = email.Trim(),
            FullName = FullName.Create(firstName, lastName),
            DateOfBirth = dateOfBirth,
            PhoneNumber = phoneNumber
        };

        user._domainEvents.Add(new UserRegisteredDomainEvent(user.Id, user.Email));
        return user;
    }

    public void UpdateProfile(string firstName, string lastName, DateOnly? dateOfBirth, string? phoneNumber)
    {
        FullName = FullName.Create(firstName, lastName);
        DateOfBirth = dateOfBirth;
        PhoneNumber = phoneNumber;
    }

    public void UpdateName(string firstName, string lastName) => FullName = FullName.Create(firstName, lastName);

    public void Activate() => IsActive = true;

    public void Deactivate()
    {
        IsActive = false;
        EndSession();
    }

    // ---------- OTP ----------

    public void IssueOtp(string code, DateTime expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        OtpCode = code;
        OtpExpiresAtUtc = expiresAtUtc;
        OtpFailedAttempts = 0;
    }

    /// <summary>Checks the code. A wrong code counts as a failed attempt; after MaxOtpAttempts the code is discarded.</summary>
    public bool TryConsumeOtp(string code, DateTime utcNow)
    {
        if (OtpCode is null || OtpExpiresAtUtc is null || OtpExpiresAtUtc <= utcNow)
        {
            ClearOtp();
            return false;
        }

        if (!FixedTimeEquals(OtpCode, code))
        {
            OtpFailedAttempts++;
            if (OtpFailedAttempts >= MaxOtpAttempts)
                ClearOtp();
            return false;
        }

        ClearOtp();
        return true;
    }

    private void ClearOtp()
    {
        OtpCode = null;
        OtpExpiresAtUtc = null;
        OtpFailedAttempts = 0;
    }

    private static bool FixedTimeEquals(string expected, string actual) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(expected), System.Text.Encoding.UTF8.GetBytes(actual ?? string.Empty));

    // ---------- Session ----------

    public bool HasActiveSession(DateTime utcNow) =>
        ActiveSessionId is not null && ActiveSessionExpiresAtUtc > utcNow;

    /// <summary>Starts a new session; any token from a previous session stops being accepted.</summary>
    public Guid StartSession(DateTime expiresAtUtc, DateTime utcNow)
    {
        ActiveSessionId = Guid.CreateVersion7();
        ActiveSessionExpiresAtUtc = expiresAtUtc;
        LastActiveAtUtc = utcNow;
        return ActiveSessionId.Value;
    }

    public bool IsSessionValid(Guid sessionId, DateTime utcNow) =>
        IsActive && ActiveSessionId == sessionId && ActiveSessionExpiresAtUtc > utcNow;

    /// <summary>
    /// Extends the current session (same session id) to a new end time. Only a session that is still valid
    /// can be renewed: an ended, replaced or expired session, or an inactive user, needs a new login.
    /// </summary>
    public bool RenewSession(Guid sessionId, DateTime newExpiresAtUtc, DateTime utcNow)
    {
        if (!IsSessionValid(sessionId, utcNow) || newExpiresAtUtc <= utcNow)
            return false;

        ActiveSessionExpiresAtUtc = newExpiresAtUtc;
        LastActiveAtUtc = utcNow;
        return true;
    }

    public void EndSession()
    {
        ActiveSessionId = null;
        ActiveSessionExpiresAtUtc = null;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    void IAuditableEntity.SetCreated(DateTime utcNow) => CreatedAtUtc = utcNow;

    void IAuditableEntity.SetModified(DateTime utcNow) => ModifiedAtUtc = utcNow;
}
