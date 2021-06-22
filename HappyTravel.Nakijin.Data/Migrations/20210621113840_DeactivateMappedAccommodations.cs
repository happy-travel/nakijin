using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class DeactivateMappedAccommodations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE ""Accommodations"" SET ""IsActive"" = 'f', ""DeactivationReason"" = 4
            WHERE ""IsActive"" = 't' and  ""Id"" in (SELECT jsonb_array_elements( ham.""MappedHtIds"")::integer FROM ""HtAccommodationMappings"" ham)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
