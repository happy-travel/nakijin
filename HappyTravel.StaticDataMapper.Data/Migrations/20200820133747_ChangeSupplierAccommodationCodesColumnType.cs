using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.StaticDataMapper.Data.Migrations
{
    public partial class ChangeSupplierAccommodationCodesColumnType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierAccommodationCodes",
                table: "Accommodations");

            migrationBuilder.AddColumn<string>(
                name: "SupplierAccommodationCodes",
                table: "Accommodations",
                type: "jsonb",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierAccommodationCodes",
                table: "Accommodations");

            migrationBuilder.AddColumn<string>(
                name: "SupplierAccommodationCodes",
                table: "Accommodations",
                type: "text",
                nullable: false);
        }
    }
}