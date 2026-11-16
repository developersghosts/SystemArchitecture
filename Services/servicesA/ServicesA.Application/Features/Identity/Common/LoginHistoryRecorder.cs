namespace ServicesA.Application.Features.Identity.Common;

/// <summary>Adds a LoginHistory row for the current request (saved with the next SaveChanges).</summary>
public sealed class LoginHistoryRecorder(IAppDbContext db, ICurrentUserService currentUser)
{
    public void Record(Guid userId, bool succeeded) =>
        db.LoginHistories.Add(LoginHistory.Create(userId, succeeded, currentUser.IpAddress, currentUser.UserAgent));
}
