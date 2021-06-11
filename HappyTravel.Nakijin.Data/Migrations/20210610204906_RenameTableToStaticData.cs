using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class RenameTableToStaticData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StaticDatas",
                table: "StaticDatas");

            migrationBuilder.RenameTable(
                name: "StaticDatas",
                newName: "StaticData");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaticData",
                table: "StaticData",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StaticData",
                table: "StaticData");

            migrationBuilder.RenameTable(
                name: "StaticData",
                newName: "StaticDatas");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaticDatas",
                table: "StaticDatas",
                column: "Id");
        }
    }
}
