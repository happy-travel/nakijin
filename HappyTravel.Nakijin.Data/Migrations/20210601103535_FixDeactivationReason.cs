using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Nakijin.Data.Migrations
{
    public partial class FixDeactivationReason : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE ""Accommodations"" SET ""DeactivationReason"" = 4
            WHERE ""DeactivationReason"" = 0 and ""IsActive"" = 'f'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
