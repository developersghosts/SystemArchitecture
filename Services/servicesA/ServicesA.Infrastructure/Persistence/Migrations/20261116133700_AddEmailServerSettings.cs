using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicesA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailServerSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailServerSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SmtpServer = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Security = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ProtectedPassword = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SenderEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResetPasswordUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailServerSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailServerSettings");
        }
    }
}
