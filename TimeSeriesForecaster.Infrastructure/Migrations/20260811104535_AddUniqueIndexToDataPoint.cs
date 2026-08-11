using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSeriesForecaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToDataPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataPoints_DatasetId",
                table: "DataPoints");

            migrationBuilder.CreateIndex(
                name: "IX_DataPoints_DatasetId_Timestamp",
                table: "DataPoints",
                columns: new[] { "DatasetId", "Timestamp" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataPoints_DatasetId_Timestamp",
                table: "DataPoints");

            migrationBuilder.CreateIndex(
                name: "IX_DataPoints_DatasetId",
                table: "DataPoints",
                column: "DatasetId");
        }
    }
}
