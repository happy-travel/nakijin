using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.StaticDataMapper.Data.Migrations
{
    public partial class AddDateTimeColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "RawAccommodations",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "Modified",
                table: "RawAccommodations",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "LocalityZones",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "Modified",
                table: "LocalityZones",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Localities",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "Modified",
                table: "Localities",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Countries",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "Modified",
                table: "Countries",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "AccommodationUncertainMatches",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "Modified",
                table: "AccommodationUncertainMatches",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Accommodations",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");

            migrationBuilder.AddColumn<DateTime>(
                name: "Modified",
                table: "Accommodations",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "RawAccommodations");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "RawAccommodations");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "LocalityZones");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "LocalityZones");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "Localities");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "Localities");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "AccommodationUncertainMatches");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "AccommodationUncertainMatches");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "Accommodations");
        }
    }
}
