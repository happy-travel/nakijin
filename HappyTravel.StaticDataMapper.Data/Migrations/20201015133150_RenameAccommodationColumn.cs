using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.StaticDataMapper.Data.Migrations
{
    public partial class RenameAccommodationColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Accommodation",
                table: "Accommodations",
                newName: "CalculatedAccommodation");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CalculatedAccommodation",
                table: "Accommodations",
                newName: "Accommodation");
        }
    }
}
