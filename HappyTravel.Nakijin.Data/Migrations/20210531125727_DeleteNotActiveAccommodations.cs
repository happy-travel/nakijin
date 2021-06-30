using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class DeleteNotActiveAccommodations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete not active accommodations, which are not valid
            migrationBuilder.Sql(@"DELETE FROM ""Accommodations"" 
            WHERE ""IsActive"" = 'f' AND ""Id"" NOT IN
            (SELECT jsonb_array_elements( ham.""MappedHtIds"")::integer FROM ""HtAccommodationMappings"" ham )");
            
            // Set correct matched accommodations deactivation reason 
            migrationBuilder.Sql(@"UPDATE ""Accommodations"" SET ""DeactivationReason"" = 4
            WHERE ""Id"" in (SELECT jsonb_array_elements( ham.""MappedHtIds"")::integer FROM ""HtAccommodationMappings"" ham)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
