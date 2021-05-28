using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class AddedForeignKeyToHtMappings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_HtAccommodationMappings_HtId",
                table: "HtAccommodationMappings",
                column: "HtId");

            migrationBuilder.AddForeignKey(
                name: "FK_HtAccommodationMappings_Accommodations_HtId",
                table: "HtAccommodationMappings",
                column: "HtId",
                principalTable: "Accommodations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HtAccommodationMappings_Accommodations_HtId",
                table: "HtAccommodationMappings");

            migrationBuilder.DropIndex(
                name: "IX_HtAccommodationMappings_HtId",
                table: "HtAccommodationMappings");
        }
    }
}
