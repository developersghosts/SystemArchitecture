using Microsoft.AspNetCore.Identity;

namespace ServicesA.Domain.Entities.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    /// <summary>Display title shown to users (Name is the technical key used in [Authorize(Roles = ...)]).</summary>
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // Required by EF Core.
    private ApplicationRole() { }

    public static ApplicationRole Create(string name, string? title = null, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ApplicationRole
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            NormalizedName = name.Trim().ToUpperInvariant(),
            Title = string.IsNullOrWhiteSpace(title) ? name.Trim() : title.Trim(),
            Description = description,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
    }

    public void UpdateTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title.Trim();
    }

    public void UpdateDescription(string? description) => Description = description;
}
