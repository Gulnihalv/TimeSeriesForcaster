using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSeriesForecaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddForecastProgressTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ForecastCompletedAt",
                table: "Models",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForecastErrorMessage",
                table: "Models",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ForecastProgressPercentage",
                table: "Models",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ForecastStartedAt",
                table: "Models",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ForecastStatus",
                table: "Models",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForecastCompletedAt",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "ForecastErrorMessage",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "ForecastProgressPercentage",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "ForecastStartedAt",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "ForecastStatus",
                table: "Models");
        }
    }
}
