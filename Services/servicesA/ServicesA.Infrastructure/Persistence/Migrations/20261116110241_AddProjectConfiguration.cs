using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicesA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultLanguage = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    DefaultTheme = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DarkBackgroundColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    LightBackgroundColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    CompanyAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompanyEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CompanyLogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CompanyTaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyWebsite = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CurrencyDecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    CurrencySymbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BaseFontSize = table.Column<int>(type: "int", nullable: false),
                    FontFamilyAr = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FontFamilyEn = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectConfigurations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectConfigurations");
        }
    }
}
