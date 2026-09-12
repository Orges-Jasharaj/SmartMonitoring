using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvironmentalMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BatteryLevelPct",
                table: "TemperatureReadings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Co2Ppm",
                table: "TemperatureReadings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HumidityPct",
                table: "TemperatureReadings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LightLevelLux",
                table: "TemperatureReadings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NoiseLevelDb",
                table: "TemperatureReadings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxCo2Ppm",
                table: "Devices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 1000m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxHumidityPct",
                table: "Devices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 70m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxLightLevelLux",
                table: "Devices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 500m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxNoiseLevelDb",
                table: "Devices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 70m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinBatteryPct",
                table: "Devices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 20m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinHumidityPct",
                table: "Devices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 40m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatteryLevelPct",
                table: "TemperatureReadings");

            migrationBuilder.DropColumn(
                name: "Co2Ppm",
                table: "TemperatureReadings");

            migrationBuilder.DropColumn(
                name: "HumidityPct",
                table: "TemperatureReadings");

            migrationBuilder.DropColumn(
                name: "LightLevelLux",
                table: "TemperatureReadings");

            migrationBuilder.DropColumn(
                name: "NoiseLevelDb",
                table: "TemperatureReadings");

            migrationBuilder.DropColumn(
                name: "MaxCo2Ppm",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "MaxHumidityPct",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "MaxLightLevelLux",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "MaxNoiseLevelDb",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "MinBatteryPct",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "MinHumidityPct",
                table: "Devices");
        }
    }
}
