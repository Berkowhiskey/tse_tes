using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TES.Domain.Entities;
using TES.Domain.Sabitler;
using TES.Infrastructure.Data;
using TES.Infrastructure.Identity;
using TES.Infrastructure.Services;

namespace TES.Tests;

/// <summary>
/// Toplu içe aktarma servisinin uçtan uca davranışı: gerçek UserManager (SQLite üstünde) ile
/// stajyer + profil oluşturma, amirsiz kayıt, mevcut Kart No atlanması, .xlsx okuma (metin + tarih hücresi),
/// ve şablon üretimi. T.C. yalnızca geçici parola olur.
/// </summary>
public class TopluStajyerServisiTests : IDisposable
{
    private readonly TestVeritabani _veritabani = new();
    private readonly TopluStajyerServisi _servis;

    public TopluStajyerServisiTests()
    {
        // AddToRoleAsync için Stajyer rolü tabloda olmalı.
        _veritabani.Db.Roles.Add(new IdentityRole { Name = Roller.Stajyer, NormalizedName = "STAJYER" });
        _veritabani.Db.SaveChanges();

        _servis = new TopluStajyerServisi(
            _veritabani.Db, UserManagerKur(_veritabani.Db), new DenetimServisi(_veritabani.Db));
    }

    [Fact]
    public async Task GecerliDosya_StajyerleriAmirsizEkler()
    {
        // Bir satır metin-tarih, bir satır gerçek tarih hücresi → iki ayrıştırma yolu da denenir.
        using var akis = XlsxYap(
            ["Ayşe", "Yılmaz", "12345678901", "3001", "01.07.2026", "28.08.2026"],
            ["Mehmet", "Kaya", "10000000146", "3002", new DateTime(2026, 7, 1), new DateTime(2026, 8, 28)]);

        var sonuc = await _servis.IceAktarAsync(akis, "admin");

        Assert.Equal(2, sonuc.EklenenSayisi);
        Assert.Empty(sonuc.Hatalar);

        var profiller = await _veritabani.Db.StajyerProfilleri.ToListAsync();
        Assert.Equal(2, profiller.Count);
        Assert.All(profiller, p => Assert.Null(p.AmirId));       // amirsiz
        Assert.All(profiller, p => Assert.Null(p.DepartmanId));

        var kullanici = await _veritabani.Db.Users.FirstAsync(u => u.UserName == "ayse_yilmaz_3001");
        Assert.True(kullanici.ForceChangePassword);              // ilk girişte parola değişimi
        Assert.Contains("3002", profiller.Select(p => p.KartNo));
    }

    [Fact]
    public async Task MevcutKartNo_AtlanirDigerleriEklenir()
    {
        // Mevcut bir stajyer (Kart No 1001).
        var mevcutId = _veritabani.KullaniciEkle("mevcut_stajyer_1001", "Mevcut Stajyer");
        _veritabani.Db.StajyerProfilleri.Add(new StajyerProfil
        {
            KullaniciId = mevcutId,
            KartNo = "1001",
            StajBaslangic = new DateOnly(2026, 7, 1),
            StajBitis = new DateOnly(2026, 8, 28)
        });
        await _veritabani.Db.SaveChangesAsync();

        using var akis = XlsxYap(
            ["Zeynep", "Ak", "12345678901", "1001", "01.07.2026", "28.08.2026"],  // çakışan kart
            ["Can", "Er", "10000000146", "1002", "01.07.2026", "28.08.2026"]);     // geçerli

        var sonuc = await _servis.IceAktarAsync(akis, "admin");

        Assert.Equal(1, sonuc.EklenenSayisi);
        var hata = Assert.Single(sonuc.Hatalar);
        Assert.Equal(2, hata.SatirNo);
        Assert.Contains("atanmış", hata.Neden);
        Assert.True(await _veritabani.Db.Users.AnyAsync(u => u.UserName == "can_er_1002"));
    }

    [Fact]
    public async Task BozukDosya_DosyaHatasiDoner()
    {
        using var akis = new MemoryStream("bu bir xlsx değil"u8.ToArray());

        var sonuc = await _servis.IceAktarAsync(akis, "admin");

        Assert.Equal(0, sonuc.EklenenSayisi);
        Assert.Equal(0, Assert.Single(sonuc.Hatalar).SatirNo); // dosya geneli hata
    }

    [Fact]
    public void Sablon_BeklenenBasliklariIcerir()
    {
        var bytes = _servis.SablonUret();
        Assert.NotEmpty(bytes);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet(StajyerSablonu.SayfaAdi);

        Assert.Equal("Ad", ws.Cell(1, 1).GetString());
        Assert.Equal("T.C. Kimlik No", ws.Cell(1, 3).GetString());
        Assert.Equal("Bölüm", ws.Cell(1, 8).GetString());
        Assert.True(ws.Cell(2, 1).IsEmpty()); // örnek veri satırı yok
    }

    // ---- yardımcılar ----

    /// <summary>Verilen satırlardan (6 çekirdek sütun) bellek-içi .xlsx üretir. string→metin, DateTime→tarih hücresi.</summary>
    private static MemoryStream XlsxYap(params object?[][] satirlar)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(StajyerSablonu.SayfaAdi);

        for (var i = 0; i < StajyerSablonu.Basliklar.Length; i++)
            ws.Cell(1, i + 1).Value = StajyerSablonu.Basliklar[i];

        var r = 2;
        foreach (var satir in satirlar)
        {
            for (var c = 0; c < satir.Length; c++)
            {
                var deger = satir[c];
                if (deger is null) continue;
                var hucre = ws.Cell(r, c + 1);
                if (deger is DateTime dt) hucre.Value = dt;
                else hucre.Value = deger.ToString();
            }
            r++;
        }

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// SQLite üstünde gerçek UserManager kurar (uygulamadaki gibi DI ile — böylece Identity store
    /// aynı DbContext örneğini kullanır). Parola politikası uygulamayla uyumlu: 11 hane, yalnız rakam.
    /// </summary>
    private static UserManager<Kullanici> UserManagerKur(TesDbContext db)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db); // testteki aynı context örneği kullanılsın
        services.AddIdentityCore<Kullanici>(o =>
        {
            o.Password.RequiredLength = 11;
            o.Password.RequireDigit = true;
            o.Password.RequireLowercase = false;
            o.Password.RequireUppercase = false;
            o.Password.RequireNonAlphanumeric = false;
            o.Password.RequiredUniqueChars = 1;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<TesDbContext>();

        return services.BuildServiceProvider().GetRequiredService<UserManager<Kullanici>>();
    }

    public void Dispose() => _veritabani.Dispose();
}
