using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSeriesForecaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSkippedRowCountToDataset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SkippedRowCount",
                table: "Datasets",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkippedRowCount",
                table: "Datasets");
        }
    }
}
