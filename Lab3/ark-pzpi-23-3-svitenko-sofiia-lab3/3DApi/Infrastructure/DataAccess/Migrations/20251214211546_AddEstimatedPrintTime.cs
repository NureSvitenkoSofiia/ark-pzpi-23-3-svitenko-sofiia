using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3DApi.Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimatedPrintTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EstimatedPrintTimeMinutes",
                table: "PrintJobs",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedPrintTimeMinutes",
                table: "PrintJobs");
        }
    }
}
