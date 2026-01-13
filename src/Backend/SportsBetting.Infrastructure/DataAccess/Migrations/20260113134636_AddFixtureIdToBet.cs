using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFixtureIdToBet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FixtureId",
                table: "Bets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FixtureId",
                table: "Bets");
        }
    }
}
