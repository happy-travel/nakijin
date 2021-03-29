using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class AddLocationDataToAccommodation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Accommodations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LocalityId",
                table: "Accommodations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LocalityZoneId",
                table: "Accommodations",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Accommodations");
                                                                                                                                                               
            migrationBuilder.DropColumn(
                name: "LocalityId",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "LocalityZoneId",
                table: "Accommodations");
        }
    }
}
