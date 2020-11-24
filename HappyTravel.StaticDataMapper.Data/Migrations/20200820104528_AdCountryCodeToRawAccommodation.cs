using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.StaticDataMapper.Data.Migrations
{
    public partial class AdCountryCodeToRawAccommodation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "RawAccommodations",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "RawAccommodations");
        }
    }
}
