using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Faz3IsTakibi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Odevler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AmirProfilId = table.Column<int>(type: "int", nullable: false),
                    StajyerProfilId = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    Ilerleme = table.Column<int>(type: "int", nullable: false),
                    TeslimTarihi = table.Column<DateOnly>(type: "date", nullable: true),
                    OlusturmaZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeZamani = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Odevler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Odevler_AmirProfilleri_AmirProfilId",
                        column: x => x.AmirProfilId,
                        principalTable: "AmirProfilleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Odevler_StajyerProfilleri_StajyerProfilId",
                        column: x => x.StajyerProfilId,
                        principalTable: "StajyerProfilleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StajyerProfilId = table.Column<int>(type: "int", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    Ilerleme = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeZamani = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projeler_StajyerProfilleri_StajyerProfilId",
                        column: x => x.StajyerProfilId,
                        principalTable: "StajyerProfilleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Odevler_AmirProfilId",
                table: "Odevler",
                column: "AmirProfilId");

            migrationBuilder.CreateIndex(
                name: "IX_Odevler_StajyerProfilId",
                table: "Odevler",
                column: "StajyerProfilId");

            migrationBuilder.CreateIndex(
                name: "IX_Projeler_StajyerProfilId",
                table: "Projeler",
                column: "StajyerProfilId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Odevler");

            migrationBuilder.DropTable(
                name: "Projeler");
        }
    }
}
