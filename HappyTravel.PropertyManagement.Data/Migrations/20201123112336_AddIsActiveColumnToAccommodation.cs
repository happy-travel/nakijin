using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.PropertyManagement.Data.Migrations
{
    public partial class AddIsActiveColumnToAccommodation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Accommodations");
        }
    }
}
