using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Application.Features.Settings.Commands.UpdateProjectConfiguration;

/// <summary>Saves the configuration (creates the single row the first time).</summary>
public sealed record UpdateProjectConfigurationCommand(
    CompanyDetailsDto Company,
    CurrencyDto Currency,
    ColorsDto Colors,
    FontsDto Fonts,
    string DefaultLanguage,
    string DefaultTheme) : ICommand<ProjectConfigurationDto>;

public sealed class UpdateProjectConfigurationValidator : AbstractValidator<UpdateProjectConfigurationCommand>
{
    /// <summary>About 200 KB of image once base64 encoded.</summary>
    private const int MaxLogoLength = 280_000;

    private static readonly string[] AllowedLogoPrefixes =
        ["https://", "http://", "data:image/png;base64,", "data:image/jpeg;base64,", "data:image/webp;base64,", "data:image/gif;base64,"];

    public UpdateProjectConfigurationValidator()
    {
        RuleFor(x => x.Company).NotNull();
        RuleFor(x => x.Currency).NotNull();
        RuleFor(x => x.Colors).NotNull();
        RuleFor(x => x.Fonts).NotNull();

        When(x => x.Company is not null, () =>
        {
            RuleFor(x => x.Company.NameEn).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Company.NameAr).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Company.Email).EmailAddress().MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.Company.Email));
            RuleFor(x => x.Company.Phone).MaximumLength(30).Matches(@"^\+?[0-9 ()-]{5,30}$")
                .When(x => !string.IsNullOrWhiteSpace(x.Company.Phone));
            RuleFor(x => x.Company.Address).MaximumLength(500);
            RuleFor(x => x.Company.Website).MaximumLength(256)
                .Must(w => Uri.TryCreate(w, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
                .When(x => !string.IsNullOrWhiteSpace(x.Company.Website))
                .WithMessage("'Website' must be an http(s) URL.");
            RuleFor(x => x.Company.TaxNumber).MaximumLength(50);
            RuleFor(x => x.Company.Country)
                .Must(c => CompanyDetails.SupportedCountries.Contains(c!.Trim().ToUpperInvariant()))
                .When(x => !string.IsNullOrWhiteSpace(x.Company.Country))
                .WithMessage("'Country' must be EG (Egypt) or SA (Saudi Arabia), or empty for standard Arabic.");
            RuleFor(x => x.Company.LogoUrl).MaximumLength(MaxLogoLength)
                .Must(l => AllowedLogoPrefixes.Any(p => l!.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                .When(x => !string.IsNullOrWhiteSpace(x.Company.LogoUrl))
                .WithMessage("'Logo' must be an http(s) URL or a PNG/JPEG/WEBP/GIF image.");
        });

        When(x => x.Currency is not null, () =>
        {
            RuleFor(x => x.Currency.Code).NotEmpty().Matches("^[A-Za-z]{3}$");
            RuleFor(x => x.Currency.Symbol).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Currency.DecimalPlaces).InclusiveBetween(0, 4);
        });

        When(x => x.Colors is not null, () =>
        {
            RuleFor(x => x.Colors.Primary).Must(ThemeColors.IsHexColor).WithMessage("'Primary' must be a #RRGGBB color.");
            RuleFor(x => x.Colors.LightBackground).Must(ThemeColors.IsHexColor).WithMessage("'Light background' must be a #RRGGBB color.");
            RuleFor(x => x.Colors.DarkBackground).Must(ThemeColors.IsHexColor).WithMessage("'Dark background' must be a #RRGGBB color.");
        });

        When(x => x.Fonts is not null, () =>
        {
            RuleFor(x => x.Fonts.FontFamilyEn).Must(Typography.IsValidFontName).WithMessage("'English font' is not a valid font name.");
            RuleFor(x => x.Fonts.FontFamilyAr).Must(Typography.IsValidFontName).WithMessage("'Arabic font' is not a valid font name.");
            RuleFor(x => x.Fonts.BaseFontSize).InclusiveBetween(Typography.MinFontSize, Typography.MaxFontSize);
        });

        RuleFor(x => x.DefaultLanguage).Must(l => l is ProjectConfiguration.LanguageEnglish or ProjectConfiguration.LanguageArabic)
            .WithMessage("'Default language' must be en or ar.");
        RuleFor(x => x.DefaultTheme).Must(t => t is ProjectConfiguration.ThemeLight or ProjectConfiguration.ThemeDark)
            .WithMessage("'Default theme' must be light or dark.");
    }
}

public sealed class UpdateProjectConfigurationHandler(IAppDbContext db, EmailBrandingProvider emailBranding,
    ILocalizedTextCache textCache)
    : ICommandHandler<UpdateProjectConfigurationCommand, ProjectConfigurationDto>
{
    public async Task<ProjectConfigurationDto> Handle(UpdateProjectConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        var configuration = await db.ProjectConfigurations
            .OrderBy(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (configuration is null)
        {
            configuration = ProjectConfiguration.CreateDefault();
            db.ProjectConfigurations.Add(configuration);
        }

        var c = request.Company;
        configuration.Update(
            CompanyDetails.Create(c.NameEn, c.NameAr, c.Email, c.Phone, c.Address, c.Website, c.TaxNumber, c.LogoUrl,
                c.Country),
            CurrencySettings.Create(request.Currency.Code, request.Currency.Symbol, request.Currency.DecimalPlaces),
            ThemeColors.Create(request.Colors.Primary, request.Colors.LightBackground, request.Colors.DarkBackground),
            Typography.Create(request.Fonts.FontFamilyEn, request.Fonts.FontFamilyAr, request.Fonts.BaseFontSize),
            request.DefaultLanguage,
            request.DefaultTheme);

        await db.SaveChangesAsync(cancellationToken);
        emailBranding.Invalidate(); // the next email uses the new name, colors and logo
        textCache.Invalidate();     // the country may have changed: messages switch dialect
        return configuration.ToDto();
    }
}
