using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.StaticDataMapper.Data.Migrations
{
    public partial class AddedMappingDataColumnToAccommodation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataForMapping",
                table: "Accommodations",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataForMapping",
                table: "Accommodations");
        }
    }
}
