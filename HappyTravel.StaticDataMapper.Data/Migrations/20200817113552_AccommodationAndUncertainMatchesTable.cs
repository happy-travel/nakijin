using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HappyTravel.StaticDataMapper.Data.Migrations
{
    public partial class AccommodationAndUncertainMatchesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accommodations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(nullable: false),
                    CountryCode = table.Column<string>(nullable: false),
                    Address = table.Column<string>(nullable: false),
                    Coordinates = table.Column<Point>(nullable: false),
                    Rating = table.Column<int>(nullable: false),
                    ContactInfo = table.Column<string>(type: "jsonb", nullable: false),
                    SupplierAccommodationCodes = table.Column<string>(nullable: false),
                    AccommodationDetails = table.Column<JsonDocument>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accommodations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccommodationUncertainMatches",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExistingHtId = table.Column<int>(nullable: false),
                    NewHtId = table.Column<int>(nullable: false),
                    Score = table.Column<float>(nullable: false),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccommodationUncertainMatches", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accommodations");

            migrationBuilder.DropTable(
                name: "AccommodationUncertainMatches");
        }
    }
}
