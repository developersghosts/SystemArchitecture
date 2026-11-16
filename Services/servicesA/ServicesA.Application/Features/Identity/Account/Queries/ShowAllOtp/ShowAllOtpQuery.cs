using ServicesA.Application.Features.Identity.Common;

namespace ServicesA.Application.Features.Identity.Account.Queries.ShowAllOtp;

/// <summary>Development helper to read pending codes when no SMTP server is configured. Never exposed in Production.</summary>
public sealed record ShowAllOtpQuery(string FullName) : IQuery<List<OtpDto>>;

public sealed class ShowAllOtpHandler(IAppDbContext db) : IQueryHandler<ShowAllOtpQuery, List<OtpDto>>
{
    public Task<List<OtpDto>> Handle(ShowAllOtpQuery request, CancellationToken cancellationToken)
    {
        var name = request.FullName.Trim();

        return db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.OtpCode != null &&
                        (u.FullName.FirstName.Contains(name) || u.FullName.LastName.Contains(name) || u.UserName!.Contains(name)))
            .Select(u => new OtpDto(u.FullName.FirstName + " " + u.FullName.LastName, u.Email, u.OtpCode, u.OtpExpiresAtUtc))
            .ToListAsync(cancellationToken);
    }
}
