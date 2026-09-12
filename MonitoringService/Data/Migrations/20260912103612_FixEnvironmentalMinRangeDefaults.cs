using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringService.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixEnvironmentalMinRangeDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill rows created before AddEnvironmentalMinRanges used zero defaults.
            migrationBuilder.Sql("""
                UPDATE Devices SET MinCo2Ppm = 400 WHERE MinCo2Ppm = 0;
                UPDATE Devices SET MinNoiseLevelDb = 35 WHERE MinNoiseLevelDb = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data fix only; no schema rollback.
        }
    }
}
