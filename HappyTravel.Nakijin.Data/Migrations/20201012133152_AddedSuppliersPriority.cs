using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class AddedSuppliersPriority : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccommodationDetails",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "ContactInfo",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "Coordinates",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Accommodations");

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                table: "Accommodations",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Accommodation",
                table: "Accommodations",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "AccommodationWithManualCorrections",
                table: "Accommodations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCalculated",
                table: "Accommodations",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SuppliersPriority",
                table: "Accommodations",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.CreateTable(
                name: "StaticDatas",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(nullable: false),
                    Value = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticDatas", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaticDatas");

            migrationBuilder.DropColumn(
                name: "Accommodation",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "AccommodationWithManualCorrections",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "IsCalculated",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "SuppliersPriority",
                table: "Accommodations");

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                table: "Accommodations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<JsonDocument>(
                name: "AccommodationDetails",
                table: "Accommodations",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Accommodations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactInfo",
                table: "Accommodations",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Point>(
                name: "Coordinates",
                table: "Accommodations",
                type: "geometry",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Accommodations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Accommodations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
