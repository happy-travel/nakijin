using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.PropertyManagement.Data.Migrations
{
    public partial class ChangeColumnRequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AccommodationWithManualCorrections",
                table: "Accommodations",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AccommodationWithManualCorrections",
                table: "Accommodations",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");
        }
    }
}
