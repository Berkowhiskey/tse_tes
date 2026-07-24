using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Faz4Sosyal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gonderiler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YazarId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ModerasyonDurumu = table.Column<int>(type: "int", nullable: false),
                    RedMesaji = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OlusturmaZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModerasyonZamani = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModeratorAdi = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gonderiler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Begeniler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GonderiId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OlusturmaZamani = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Begeniler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Begeniler_Gonderiler_GonderiId",
                        column: x => x.GonderiId,
                        principalTable: "Gonderiler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Yorumlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GonderiId = table.Column<int>(type: "int", nullable: false),
                    YazarId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OlusturmaZamani = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Yorumlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Yorumlar_Gonderiler_GonderiId",
                        column: x => x.GonderiId,
                        principalTable: "Gonderiler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Begeniler_GonderiId_KullaniciId",
                table: "Begeniler",
                columns: new[] { "GonderiId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gonderiler_ModerasyonDurumu",
                table: "Gonderiler",
                column: "ModerasyonDurumu");

            migrationBuilder.CreateIndex(
                name: "IX_Gonderiler_YazarId",
                table: "Gonderiler",
                column: "YazarId");

            migrationBuilder.CreateIndex(
                name: "IX_Yorumlar_GonderiId",
                table: "Yorumlar",
                column: "GonderiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Begeniler");

            migrationBuilder.DropTable(
                name: "Yorumlar");

            migrationBuilder.DropTable(
                name: "Gonderiler");
        }
    }
}
