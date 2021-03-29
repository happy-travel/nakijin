using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class AddedForeignKeysToAccommodationUncertainMatches : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Accommodations",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationUncertainMatches_FirstHtId",
                table: "AccommodationUncertainMatches",
                column: "FirstHtId");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationUncertainMatches_SecondHtId",
                table: "AccommodationUncertainMatches",
                column: "SecondHtId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationUncertainMatches_Accommodations_FirstHtId",
                table: "AccommodationUncertainMatches",
                column: "FirstHtId",
                principalTable: "Accommodations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationUncertainMatches_Accommodations_SecondHtId",
                table: "AccommodationUncertainMatches",
                column: "SecondHtId",
                principalTable: "Accommodations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationUncertainMatches_Accommodations_FirstHtId",
                table: "AccommodationUncertainMatches");

            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationUncertainMatches_Accommodations_SecondHtId",
                table: "AccommodationUncertainMatches");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationUncertainMatches_FirstHtId",
                table: "AccommodationUncertainMatches");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationUncertainMatches_SecondHtId",
                table: "AccommodationUncertainMatches");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }
    }
}
