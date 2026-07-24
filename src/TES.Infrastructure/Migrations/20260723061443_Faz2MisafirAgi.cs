using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Faz2MisafirAgi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GidenEpostalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kime = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Konu = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Zaman = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GidenEpostalar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MisafirErisimTalepleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TakipKodu = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StajyerProfilId = table.Column<int>(type: "int", nullable: true),
                    AdSoyad = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Eposta = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SponsorEposta = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SureGun = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TokenSonGecerlilik = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoucherHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErisimBitis = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturmaZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KararZamani = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KarariVeren = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MisafirErisimTalepleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MisafirErisimTalepleri_StajyerProfilleri_StajyerProfilId",
                        column: x => x.StajyerProfilId,
                        principalTable: "StajyerProfilleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SimuleAgErisimleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MisafirErisimTalebiId = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    Bitis = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CihazSayisi = table.Column<int>(type: "int", nullable: false),
                    GuncellemeZamani = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimuleAgErisimleri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GidenEpostalar_Zaman",
                table: "GidenEpostalar",
                column: "Zaman");

            migrationBuilder.CreateIndex(
                name: "IX_MisafirErisimTalepleri_Durum",
                table: "MisafirErisimTalepleri",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_MisafirErisimTalepleri_SponsorEposta_OlusturmaZamani",
                table: "MisafirErisimTalepleri",
                columns: new[] { "SponsorEposta", "OlusturmaZamani" });

            migrationBuilder.CreateIndex(
                name: "IX_MisafirErisimTalepleri_StajyerProfilId",
                table: "MisafirErisimTalepleri",
                column: "StajyerProfilId");

            migrationBuilder.CreateIndex(
                name: "IX_MisafirErisimTalepleri_TakipKodu",
                table: "MisafirErisimTalepleri",
                column: "TakipKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MisafirErisimTalepleri_TokenHash",
                table: "MisafirErisimTalepleri",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimuleAgErisimleri_MisafirErisimTalebiId",
                table: "SimuleAgErisimleri",
                column: "MisafirErisimTalebiId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GidenEpostalar");

            migrationBuilder.DropTable(
                name: "MisafirErisimTalepleri");

            migrationBuilder.DropTable(
                name: "SimuleAgErisimleri");
        }
    }
}
