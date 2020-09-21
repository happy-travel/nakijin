using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.PropertyManagement.Data.Migrations
{
    public partial class ChangedSuppliersType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "RawAccommodations");

            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "RawAccommodations");

            migrationBuilder.AddColumn<int>(
                name: "Supplier",
                table: "RawAccommodations",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "SupplierAccommodationId",
                table: "RawAccommodations",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierAccommodationId",
                table: "RawAccommodations");

            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "RawAccommodations");

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "RawAccommodations",
                type: "text",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "SupplierId",
                table: "RawAccommodations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}