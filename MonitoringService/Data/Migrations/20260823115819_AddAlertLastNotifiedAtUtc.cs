using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertLastNotifiedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastNotifiedAtUtc",
                table: "Alerts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastNotifiedAtUtc",
                table: "Alerts");
        }
    }
}
