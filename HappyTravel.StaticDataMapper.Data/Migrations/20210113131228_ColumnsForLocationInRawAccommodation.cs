using HappyTravel.MultiLanguage;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.StaticDataMapper.Data.Migrations
{
    public partial class ColumnsForLocationInRawAccommodation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<MultiLanguage<string>>(
                name: "CountryNames",
                table: "RawAccommodations",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<MultiLanguage<string>>(
                name: "LocalityNames",
                table: "RawAccommodations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<MultiLanguage<string>>(
                name: "LocalityZoneNames",
                table: "RawAccommodations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierLocalityCode",
                table: "RawAccommodations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierLocalityZoneCode",
                table: "RawAccommodations",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryNames",
                table: "RawAccommodations");

            migrationBuilder.DropColumn(
                name: "LocalityNames",
                table: "RawAccommodations");

            migrationBuilder.DropColumn(
                name: "LocalityZoneNames",
                table: "RawAccommodations");

            migrationBuilder.DropColumn(
                name: "SupplierLocalityCode",
                table: "RawAccommodations");

            migrationBuilder.DropColumn(
                name: "SupplierLocalityZoneCode",
                table: "RawAccommodations");
        }
    }
}
