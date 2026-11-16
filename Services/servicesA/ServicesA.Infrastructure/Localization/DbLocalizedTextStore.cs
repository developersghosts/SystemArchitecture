using BuildingBlocks.Localization;
using Microsoft.Extensions.Logging;
using ServicesA.Application.Abstractions;
using ServicesA.Infrastructure.Persistence;

namespace ServicesA.Infrastructure.Localization;

/// <summary>
/// The message dictionary (LocalizedTexts table) and the company's Arabic dialect (Project configuration -> country),
/// loaded once into memory and kept until a save calls <see cref="Invalidate"/>. Lookups are dictionary reads,
/// so messages cost no database query. If the database can't be read, the .resx files are used and it retries in 30 s.
/// </summary>
public sealed class DbLocalizedTextStore(IServiceScopeFactory scopes, ILogger<DbLocalizedTextStore> logger)
    : ILocalizedTextStore, ILocalizedTextCache
{
    private sealed record Snapshot(IReadOnlyDictionary<string, string> Texts, string? Dialect, DateTime LoadedAtUtc, bool Failed);

    private readonly Lock _lock = new();
    private volatile Snapshot? _snapshot;

    public string? ArabicDialect => Current().Dialect;

    public string? Find(string key, string culture) =>
        Current().Texts.TryGetValue($"{culture}|{key}", out var value) ? value : null;

    public void Invalidate() => _snapshot = null;

    private Snapshot Current()
    {
        var snapshot = _snapshot;
        if (snapshot is not null && !(snapshot.Failed && DateTime.UtcNow - snapshot.LoadedAtUtc > TimeSpan.FromSeconds(30)))
            return snapshot;

        lock (_lock)
        {
            snapshot = _snapshot;
            if (snapshot is not null && !(snapshot.Failed && DateTime.UtcNow - snapshot.LoadedAtUtc > TimeSpan.FromSeconds(30)))
                return snapshot;
            _snapshot = snapshot = Load();
            return snapshot;
        }
    }

    private Snapshot Load()
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var texts = db.LocalizedTexts.AsNoTracking()
                .Select(t => new { t.Culture, t.Key, t.Value })
                .ToList()
                .ToDictionary(t => $"{t.Culture}|{t.Key}", t => t.Value);

            var country = db.ProjectConfigurations.AsNoTracking()
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => c.Company.Country)
                .FirstOrDefault();
            var dialect = country switch { "EG" => "ar-EG", "SA" => "ar-SA", _ => null };

            logger.LogInformation("Message dictionary loaded: {Count} texts, Arabic dialect {Dialect}",
                texts.Count, dialect ?? "standard");
            return new Snapshot(texts, dialect, DateTime.UtcNow, Failed: false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not load the message dictionary; using the built-in texts");
            return new Snapshot(new Dictionary<string, string>(), null, DateTime.UtcNow, Failed: true);
        }
    }
}
