using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CandlestickData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Candlesticks",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TimeFrame = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OpenTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CloseTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpenPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    HighPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    LowPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    ClosePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candlesticks", x => new { x.Symbol, x.TimeFrame, x.OpenTime });
                });

            migrationBuilder.CreateTable(
                name: "SymbolIntegrities",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TimeFrame = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    GapFromOpenTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GapToOpenTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastVerifiedOpenTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymbolIntegrities", x => new { x.Symbol, x.TimeFrame });
                });

            migrationBuilder.CreateTable(
                name: "SyncCheckpoints",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TimeFrame = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LastSyncedOpenTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncCheckpoints", x => new { x.Symbol, x.TimeFrame });
                });

            migrationBuilder.CreateTable(
                name: "SyncJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StoppedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candlesticks_Symbol_TimeFrame_OpenTime",
                table: "Candlesticks",
                columns: new[] { "Symbol", "TimeFrame", "OpenTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Candlesticks");

            migrationBuilder.DropTable(
                name: "SymbolIntegrities");

            migrationBuilder.DropTable(
                name: "SyncCheckpoints");

            migrationBuilder.DropTable(
                name: "SyncJobs");
        }
    }
}
