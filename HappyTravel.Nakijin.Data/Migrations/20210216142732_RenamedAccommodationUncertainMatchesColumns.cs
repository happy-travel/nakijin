using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class RenamedAccommodationUncertainMatchesColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NewHtId",
                table: "AccommodationUncertainMatches",
                newName: "SecondHtId");

            migrationBuilder.RenameColumn(
                name: "ExistingHtId",
                table: "AccommodationUncertainMatches",
                newName: "FirstHtId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SecondHtId",
                table: "AccommodationUncertainMatches",
                newName: "NewHtId");

            migrationBuilder.RenameColumn(
                name: "FirstHtId",
                table: "AccommodationUncertainMatches",
                newName: "ExistingHtId");
        }
    }
}
