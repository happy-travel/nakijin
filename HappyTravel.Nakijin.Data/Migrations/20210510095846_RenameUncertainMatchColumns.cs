using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class RenameUncertainMatchColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationUncertainMatches_Accommodations_FirstHtId",
                table: "AccommodationUncertainMatches");

            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationUncertainMatches_Accommodations_SecondHtId",
                table: "AccommodationUncertainMatches");

            migrationBuilder.RenameColumn(
                name: "SecondHtId",
                table: "AccommodationUncertainMatches",
                newName: "SourceHtId");

            migrationBuilder.RenameColumn(
                name: "FirstHtId",
                table: "AccommodationUncertainMatches",
                newName: "HtIdToMatch");

            migrationBuilder.RenameIndex(
                name: "IX_AccommodationUncertainMatches_SecondHtId",
                table: "AccommodationUncertainMatches",
                newName: "IX_AccommodationUncertainMatches_SourceHtId");

            migrationBuilder.RenameIndex(
                name: "IX_AccommodationUncertainMatches_FirstHtId",
                table: "AccommodationUncertainMatches",
                newName: "IX_AccommodationUncertainMatches_HtIdToMatch");

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationUncertainMatches_Accommodations_HtIdToMatch",
                table: "AccommodationUncertainMatches",
                column: "HtIdToMatch",
                principalTable: "Accommodations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationUncertainMatches_Accommodations_SourceHtId",
                table: "AccommodationUncertainMatches",
                column: "SourceHtId",
                principalTable: "Accommodations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationUncertainMatches_Accommodations_HtIdToMatch",
                table: "AccommodationUncertainMatches");

            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationUncertainMatches_Accommodations_SourceHtId",
                table: "AccommodationUncertainMatches");

            migrationBuilder.RenameColumn(
                name: "SourceHtId",
                table: "AccommodationUncertainMatches",
                newName: "SecondHtId");

            migrationBuilder.RenameColumn(
                name: "HtIdToMatch",
                table: "AccommodationUncertainMatches",
                newName: "FirstHtId");

            migrationBuilder.RenameIndex(
                name: "IX_AccommodationUncertainMatches_SourceHtId",
                table: "AccommodationUncertainMatches",
                newName: "IX_AccommodationUncertainMatches_SecondHtId");

            migrationBuilder.RenameIndex(
                name: "IX_AccommodationUncertainMatches_HtIdToMatch",
                table: "AccommodationUncertainMatches",
                newName: "IX_AccommodationUncertainMatches_FirstHtId");

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
    }
}
