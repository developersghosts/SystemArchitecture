using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Application.Features.Settings.Localization;

/// <summary>One message key with its English, Egyptian and Saudi texts.</summary>
public sealed record MessageTextDto(string Key, string? En, string? Egyptian, string? Saudi, DateTime? ModifiedAtUtc);

/// <param name="ArabicDialect">"ar-EG", "ar-SA" or null (standard Arabic), from the company's country.</param>
public sealed record MessageDictionaryDto(string? Country, string? ArabicDialect, List<MessageTextDto> Items);

public sealed record GetMessageDictionaryQuery(string? Search) : IQuery<MessageDictionaryDto>;

public sealed class GetMessageDictionaryHandler(IAppDbContext db) : IQueryHandler<GetMessageDictionaryQuery, MessageDictionaryDto>
{
    public async Task<MessageDictionaryDto> Handle(GetMessageDictionaryQuery request, CancellationToken cancellationToken)
    {
        var rows = await db.LocalizedTexts.AsNoTracking().ToListAsync(cancellationToken);
        var search = request.Search?.Trim();

        var items = rows.GroupBy(r => r.Key)
            .Select(g => new MessageTextDto(g.Key,
                g.FirstOrDefault(r => r.Culture == LocalizedText.English)?.Value,
                g.FirstOrDefault(r => r.Culture == LocalizedText.Egyptian)?.Value,
                g.FirstOrDefault(r => r.Culture == LocalizedText.Saudi)?.Value,
                g.Max(r => r.ModifiedAtUtc)))
            .Where(i => string.IsNullOrEmpty(search) ||
                        i.Key.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (i.En?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (i.Egyptian?.Contains(search) ?? false) ||
                        (i.Saudi?.Contains(search) ?? false))
            .OrderBy(i => i.Key)
            .ToList();

        var company = await db.ProjectConfigurations.AsNoTracking().OrderBy(c => c.CreatedAtUtc)
            .Select(c => new { c.Company.Country }).FirstOrDefaultAsync(cancellationToken);
        var dialect = company?.Country switch { "EG" => LocalizedText.Egyptian, "SA" => LocalizedText.Saudi, _ => null };

        return new MessageDictionaryDto(company?.Country, dialect, items);
    }
}

/// <summary>Saves the three texts of one existing message key; the change applies to the next message at once.</summary>
public sealed record UpdateMessageTextCommand(string Key, string En, string Egyptian, string Saudi) : ICommand<MessageTextDto>;

public sealed class UpdateMessageTextValidator : AbstractValidator<UpdateMessageTextCommand>
{
    public UpdateMessageTextValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
        RuleFor(x => x.En).NotEmpty().MaximumLength(LocalizedText.MaxLength);
        RuleFor(x => x.Egyptian).NotEmpty().MaximumLength(LocalizedText.MaxLength);
        RuleFor(x => x.Saudi).NotEmpty().MaximumLength(LocalizedText.MaxLength);
    }
}

public sealed class UpdateMessageTextHandler(IAppDbContext db, ILocalizedTextCache cache, IStringLocalizerService localizer)
    : ICommandHandler<UpdateMessageTextCommand, MessageTextDto>
{
    public async Task<MessageTextDto> Handle(UpdateMessageTextCommand request, CancellationToken cancellationToken)
    {
        var key = request.Key.Trim();
        var rows = await db.LocalizedTexts.Where(t => t.Key == key).ToListAsync(cancellationToken);
        if (rows.Count == 0) // only keys the system uses can be edited
            throw new NotFoundException(localizer.Get(ResourceKeys.NotFound));

        foreach (var (culture, value) in new[]
                 { (LocalizedText.English, request.En), (LocalizedText.Egyptian, request.Egyptian), (LocalizedText.Saudi, request.Saudi) })
        {
            var row = rows.FirstOrDefault(r => r.Culture == culture);
            if (row is null) db.LocalizedTexts.Add(LocalizedText.Create(key, culture, value));
            else if (row.Value != value.Trim()) row.ChangeValue(value);
        }

        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate();

        return new MessageTextDto(key, request.En.Trim(), request.Egyptian.Trim(), request.Saudi.Trim(), DateTime.UtcNow);
    }
}
