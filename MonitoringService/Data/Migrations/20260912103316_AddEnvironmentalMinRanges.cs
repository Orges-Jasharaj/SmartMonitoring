using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvironmentalMinRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinCo2Ppm",
                table: "Devices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 400m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinLightLevelLux",
                table: "Devices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinNoiseLevelDb",
                table: "Devices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 35m);

            migrationBuilder.Sql("""
                UPDATE Devices SET MinCo2Ppm = 400 WHERE MinCo2Ppm = 0;
                UPDATE Devices SET MinNoiseLevelDb = 35 WHERE MinNoiseLevelDb = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinCo2Ppm",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "MinLightLevelLux",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "MinNoiseLevelDb",
                table: "Devices");
        }
    }
}
