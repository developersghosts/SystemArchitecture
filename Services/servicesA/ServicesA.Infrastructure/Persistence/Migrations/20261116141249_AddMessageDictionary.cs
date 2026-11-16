using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicesA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageDictionary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyCountry",
                table: "ProjectConfigurations",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LocalizedTexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalizedTexts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalizedTexts_Key_Culture",
                table: "LocalizedTexts",
                columns: new[] { "Key", "Culture" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalizedTexts");

            migrationBuilder.DropColumn(
                name: "CompanyCountry",
                table: "ProjectConfigurations");
        }
    }
}
