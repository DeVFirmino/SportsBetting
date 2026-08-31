using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTransactionsAndBetIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "Bets",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BetId = table.Column<long>(type: "bigint", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Bets_BetId",
                        column: x => x.BetId,
                        principalTable: "Bets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bets_UserId_ClientRequestId",
                table: "Bets",
                columns: new[] { "UserId", "ClientRequestId" },
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_BetId",
                table: "WalletTransactions",
                column: "BetId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_UserId",
                table: "WalletTransactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Bets_UserId_ClientRequestId",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "Bets");
        }
    }
}
