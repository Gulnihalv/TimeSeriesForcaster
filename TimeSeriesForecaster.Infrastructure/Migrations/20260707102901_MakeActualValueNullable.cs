using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeSeriesForecaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeActualValueNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ActualValue",
                table: "Predictions",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Models",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Models");

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualValue",
                table: "Predictions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);
        }
    }
}
