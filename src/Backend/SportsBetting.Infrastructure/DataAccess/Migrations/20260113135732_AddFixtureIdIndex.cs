using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFixtureIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Bets_FixtureId",
                table: "Bets",
                column: "FixtureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bets_FixtureId",
                table: "Bets");
        }
    }
}
