using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Infrastructure.Persistence.Configurations;

public class ProjectConfigurationConfiguration : IEntityTypeConfiguration<ProjectConfiguration>
{
    public void Configure(EntityTypeBuilder<ProjectConfiguration> builder)
    {
        builder.ToTable("ProjectConfigurations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.ComplexProperty(c => c.Company, company =>
        {
            company.Property(p => p.NameEn).HasColumnName("CompanyNameEn").HasMaxLength(200).IsRequired();
            company.Property(p => p.NameAr).HasColumnName("CompanyNameAr").HasMaxLength(200).IsRequired();
            company.Property(p => p.Email).HasColumnName("CompanyEmail").HasMaxLength(256);
            company.Property(p => p.Phone).HasColumnName("CompanyPhone").HasMaxLength(30);
            company.Property(p => p.Address).HasColumnName("CompanyAddress").HasMaxLength(500);
            company.Property(p => p.Website).HasColumnName("CompanyWebsite").HasMaxLength(256);
            company.Property(p => p.TaxNumber).HasColumnName("CompanyTaxNumber").HasMaxLength(50);
            company.Property(p => p.LogoUrl).HasColumnName("CompanyLogoUrl"); // nvarchar(max): may hold a data URL
            company.Property(p => p.Country).HasColumnName("CompanyCountry").HasMaxLength(2);
            company.Ignore(p => p.ArabicDialect);
        });

        builder.ComplexProperty(c => c.Currency, currency =>
        {
            currency.Property(p => p.Code).HasColumnName("CurrencyCode").HasMaxLength(3).IsRequired();
            currency.Property(p => p.Symbol).HasColumnName("CurrencySymbol").HasMaxLength(10).IsRequired();
            currency.Property(p => p.DecimalPlaces).HasColumnName("CurrencyDecimalPlaces");
        });

        builder.ComplexProperty(c => c.Colors, colors =>
        {
            colors.Property(p => p.Primary).HasColumnName("PrimaryColor").HasMaxLength(7).IsRequired();
            colors.Property(p => p.LightBackground).HasColumnName("LightBackgroundColor").HasMaxLength(7).IsRequired();
            colors.Property(p => p.DarkBackground).HasColumnName("DarkBackgroundColor").HasMaxLength(7).IsRequired();
        });

        builder.ComplexProperty(c => c.Fonts, fonts =>
        {
            fonts.Property(p => p.FontFamilyEn).HasColumnName("FontFamilyEn").HasMaxLength(60).IsRequired();
            fonts.Property(p => p.FontFamilyAr).HasColumnName("FontFamilyAr").HasMaxLength(60).IsRequired();
            fonts.Property(p => p.BaseFontSize).HasColumnName("BaseFontSize");
        });

        builder.Property(c => c.DefaultLanguage).HasMaxLength(5).IsRequired();
        builder.Property(c => c.DefaultTheme).HasMaxLength(10).IsRequired();
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.Ignore(c => c.DomainEvents);
    }
}
