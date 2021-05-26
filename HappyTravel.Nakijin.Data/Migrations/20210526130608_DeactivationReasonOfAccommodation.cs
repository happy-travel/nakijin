using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class DeactivationReasonOfAccommodation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeactivationReason",
                table: "Accommodations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "Accommodations");
        }
    }
}
