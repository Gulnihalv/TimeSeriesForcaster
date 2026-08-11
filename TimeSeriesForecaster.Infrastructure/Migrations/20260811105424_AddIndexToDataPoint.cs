using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSeriesForecaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexToDataPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataPoints_DatasetId_Timestamp",
                table: "DataPoints");

            migrationBuilder.CreateIndex(
                name: "IX_DataPoints_DatasetId_Timestamp",
                table: "DataPoints",
                columns: new[] { "DatasetId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataPoints_DatasetId_Timestamp",
                table: "DataPoints");

            migrationBuilder.CreateIndex(
                name: "IX_DataPoints_DatasetId_Timestamp",
                table: "DataPoints",
                columns: new[] { "DatasetId", "Timestamp" },
                unique: true);
        }
    }
}
