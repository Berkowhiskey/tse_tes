using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Faz1KimlikOrganizasyon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departmanlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UstDepartmanId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departmanlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departmanlar_Departmanlar_UstDepartmanId",
                        column: x => x.UstDepartmanId,
                        principalTable: "Departmanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AmirProfilleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IseBaslamaTarihi = table.Column<DateOnly>(type: "date", nullable: true),
                    Hakkimda = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DepartmanId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmirProfilleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmirProfilleri_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AmirProfilleri_Departmanlar_DepartmanId",
                        column: x => x.DepartmanId,
                        principalTable: "Departmanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StajyerProfilleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    KartNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Okul = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Bolum = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StajBaslangic = table.Column<DateOnly>(type: "date", nullable: false),
                    StajBitis = table.Column<DateOnly>(type: "date", nullable: false),
                    AmirId = table.Column<int>(type: "int", nullable: true),
                    DepartmanId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StajyerProfilleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StajyerProfilleri_AmirProfilleri_AmirId",
                        column: x => x.AmirId,
                        principalTable: "AmirProfilleri",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StajyerProfilleri_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StajyerProfilleri_Departmanlar_DepartmanId",
                        column: x => x.DepartmanId,
                        principalTable: "Departmanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YoklamaKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StajyerProfilId = table.Column<int>(type: "int", nullable: false),
                    GirisZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CikisZamani = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YoklamaKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YoklamaKayitlari_StajyerProfilleri_StajyerProfilId",
                        column: x => x.StajyerProfilId,
                        principalTable: "StajyerProfilleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmirProfilleri_DepartmanId",
                table: "AmirProfilleri",
                column: "DepartmanId");

            migrationBuilder.CreateIndex(
                name: "IX_AmirProfilleri_KullaniciId",
                table: "AmirProfilleri",
                column: "KullaniciId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departmanlar_UstDepartmanId_Ad",
                table: "Departmanlar",
                columns: new[] { "UstDepartmanId", "Ad" },
                unique: true,
                filter: "[UstDepartmanId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StajyerProfilleri_AmirId",
                table: "StajyerProfilleri",
                column: "AmirId");

            migrationBuilder.CreateIndex(
                name: "IX_StajyerProfilleri_DepartmanId",
                table: "StajyerProfilleri",
                column: "DepartmanId");

            migrationBuilder.CreateIndex(
                name: "IX_StajyerProfilleri_KartNo",
                table: "StajyerProfilleri",
                column: "KartNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StajyerProfilleri_KullaniciId",
                table: "StajyerProfilleri",
                column: "KullaniciId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YoklamaKayitlari_StajyerProfilId_GirisZamani",
                table: "YoklamaKayitlari",
                columns: new[] { "StajyerProfilId", "GirisZamani" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YoklamaKayitlari");

            migrationBuilder.DropTable(
                name: "StajyerProfilleri");

            migrationBuilder.DropTable(
                name: "AmirProfilleri");

            migrationBuilder.DropTable(
                name: "Departmanlar");
        }
    }
}
