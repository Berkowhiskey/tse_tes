using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Faz5Iletisim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SohbetMesajlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GondericiId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AliciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Zaman = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OkunduMu = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SohbetMesajlari", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SohbetMesajlari_AliciId_OkunduMu",
                table: "SohbetMesajlari",
                columns: new[] { "AliciId", "OkunduMu" });

            migrationBuilder.CreateIndex(
                name: "IX_SohbetMesajlari_GondericiId_AliciId_Zaman",
                table: "SohbetMesajlari",
                columns: new[] { "GondericiId", "AliciId", "Zaman" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SohbetMesajlari");
        }
    }
}
