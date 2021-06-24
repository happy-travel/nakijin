using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class AddFkRelationsBetweenAccommodationAndLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LocalityZones_LocalityId",
                table: "LocalityZones",
                column: "LocalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Localities_CountryId",
                table: "Localities",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_CountryId",
                table: "Accommodations",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_LocalityId",
                table: "Accommodations",
                column: "LocalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_LocalityZoneId",
                table: "Accommodations",
                column: "LocalityZoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accommodations_Countries_CountryId",
                table: "Accommodations",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accommodations_Localities_LocalityId",
                table: "Accommodations",
                column: "LocalityId",
                principalTable: "Localities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accommodations_LocalityZones_LocalityZoneId",
                table: "Accommodations",
                column: "LocalityZoneId",
                principalTable: "LocalityZones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Localities_Countries_CountryId",
                table: "Localities",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalityZones_Localities_LocalityId",
                table: "LocalityZones",
                column: "LocalityId",
                principalTable: "Localities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accommodations_Countries_CountryId",
                table: "Accommodations");

            migrationBuilder.DropForeignKey(
                name: "FK_Accommodations_Localities_LocalityId",
                table: "Accommodations");

            migrationBuilder.DropForeignKey(
                name: "FK_Accommodations_LocalityZones_LocalityZoneId",
                table: "Accommodations");

            migrationBuilder.DropForeignKey(
                name: "FK_Localities_Countries_CountryId",
                table: "Localities");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalityZones_Localities_LocalityId",
                table: "LocalityZones");

            migrationBuilder.DropIndex(
                name: "IX_LocalityZones_LocalityId",
                table: "LocalityZones");

            migrationBuilder.DropIndex(
                name: "IX_Localities_CountryId",
                table: "Localities");

            migrationBuilder.DropIndex(
                name: "IX_Accommodations_CountryId",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_Accommodations_LocalityId",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_Accommodations_LocalityZoneId",
                table: "Accommodations");
        }
    }
}
