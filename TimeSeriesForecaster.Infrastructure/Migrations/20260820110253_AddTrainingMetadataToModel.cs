using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSeriesForecaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingMetadataToModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrainingAggregation",
                table: "Models",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrainingResolution",
                table: "Models",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrainingRowCount",
                table: "Models",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrainingAggregation",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "TrainingResolution",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "TrainingRowCount",
                table: "Models");
        }
    }
}
