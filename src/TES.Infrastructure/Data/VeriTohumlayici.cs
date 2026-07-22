using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TES.Domain.Entities;
using TES.Domain.Kurallar;
using TES.Domain.Sabitler;
using TES.Infrastructure.Identity;
using TES.Infrastructure.Services;

namespace TES.Infrastructure.Data;

/// <summary>
/// Geliştirme ortamı için başlangıç verisi: roller + örnek kullanıcılar.
/// Kural (CLAUDE.md Bölüm 7): ilk geçici parola = T.C. Kimlik No (hash'lenir, asla düz
/// metin saklanmaz) ve ilk girişte parola değişimi zorunludur.
/// Buradaki T.C. numaraları GERÇEK DEĞİLDİR; yalnızca geliştirme/seed amaçlı sahte değerlerdir.
/// </summary>
public static class VeriTohumlayici
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<TesDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<Kullanici>>();
        var denetim = services.GetRequiredService<IDenetimServisi>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("VeriTohumlayici");

        // 1) Roller
        foreach (var rol in Roller.Tumu)
        {
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));
        }

        // 2) Örnek kullanıcılar (ad, soyad, kartNo, sahteTc, rol)
        // Kullanıcı adı kuralı: ad_soyad_kartno (KullaniciAdiUretici). Admin için sabit "admin".
        var ornekler = new (string? SabitKullaniciAdi, string Ad, string Soyad, string? KartNo, string SahteTc, string Rol)[]
        {
            ("admin", "Sistem", "Yöneticisi", null, "10000000146", Roller.Admin),
            (null, "Mehmet", "Demir", null, "20000000148", Roller.Amir),
            (null, "Ayşe", "Yılmaz", "1001", "30000000140", Roller.Stajyer),
        };

        var yeniOlusan = false;

        foreach (var o in ornekler)
        {
            var kullaniciAdi = o.SabitKullaniciAdi ?? KullaniciAdiUretici.Uret(o.Ad, o.Soyad, o.KartNo);

            if (await userManager.FindByNameAsync(kullaniciAdi) is not null)
                continue;

            var kullanici = new Kullanici
            {
                UserName = kullaniciAdi,
                AdSoyad = $"{o.Ad} {o.Soyad}",
                ForceChangePassword = true // ilk girişte parola değişimi zorunlu
            };

            // Geçici parola sahte T.C.'dir; Identity tarafından hash'lenir, hiçbir yerde loglanmaz.
            var sonuc = await userManager.CreateAsync(kullanici, o.SahteTc);

            if (!sonuc.Succeeded)
            {
                logger.LogError("Seed kullanıcısı oluşturulamadı: {KullaniciAdi} — {Hatalar}",
                    kullaniciAdi, string.Join("; ", sonuc.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(kullanici, o.Rol);
            yeniOlusan = true;
        }

        if (yeniOlusan)
            await denetim.KaydetAsync("Sistem", "SeedTamamlandi", "Roller ve örnek kullanıcılar oluşturuldu.");

        // 3) Faz 1: Departman hiyerarşisi + örnek profiller
        await Faz1SeedAsync(db, userManager, denetim);
    }

    /// <summary>Departman hiyerarşisi ve örnek amir/stajyer profilleri (idempotent).</summary>
    private static async Task Faz1SeedAsync(TesDbContext db, UserManager<Kullanici> userManager, IDenetimServisi denetim)
    {
        var yeniOlusan = false;

        // Departmanlar: Bilgi-İşlem → { Yazılım Geliştirme, Sistem, Donanım }
        var bilgiIslem = await db.Departmanlar.FirstOrDefaultAsync(d => d.Ad == "Bilgi-İşlem" && d.UstDepartmanId == null);
        if (bilgiIslem is null)
        {
            bilgiIslem = new Departman { Ad = "Bilgi-İşlem" };
            db.Departmanlar.Add(bilgiIslem);
            await db.SaveChangesAsync();

            db.Departmanlar.AddRange(
                new Departman { Ad = "Yazılım Geliştirme", UstDepartmanId = bilgiIslem.Id },
                new Departman { Ad = "Sistem", UstDepartmanId = bilgiIslem.Id },
                new Departman { Ad = "Donanım", UstDepartmanId = bilgiIslem.Id });
            await db.SaveChangesAsync();
            yeniOlusan = true;
        }

        var yazilimGelistirme = await db.Departmanlar.FirstAsync(d => d.Ad == "Yazılım Geliştirme");

        // Amir profili (mehmet_demir → Yazılım Geliştirme)
        var amirKullanici = await userManager.FindByNameAsync("mehmet_demir");
        AmirProfil? amirProfil = null;
        if (amirKullanici is not null)
        {
            amirProfil = await db.AmirProfilleri.FirstOrDefaultAsync(a => a.KullaniciId == amirKullanici.Id);
            if (amirProfil is null)
            {
                amirProfil = new AmirProfil
                {
                    KullaniciId = amirKullanici.Id,
                    DepartmanId = yazilimGelistirme.Id,
                    IseBaslamaTarihi = new DateOnly(2020, 3, 16),
                    Hakkimda = "Yazılım Geliştirme biriminde kıdemli mühendis."
                };
                db.AmirProfilleri.Add(amirProfil);
                await db.SaveChangesAsync();
                yeniOlusan = true;
            }
        }

        // Stajyer profili (ayse_yilmaz_1001, kart 1001, amiri mehmet_demir)
        var stajyerKullanici = await userManager.FindByNameAsync("ayse_yilmaz_1001");
        if (stajyerKullanici is not null &&
            !await db.StajyerProfilleri.AnyAsync(s => s.KullaniciId == stajyerKullanici.Id))
        {
            db.StajyerProfilleri.Add(new StajyerProfil
            {
                KullaniciId = stajyerKullanici.Id,
                KartNo = "1001",
                Okul = "Örnek Üniversitesi",
                Bolum = "Bilgisayar Mühendisliği",
                StajBaslangic = new DateOnly(2026, 7, 1),
                StajBitis = new DateOnly(2026, 8, 28),
                AmirId = amirProfil?.Id,
                DepartmanId = amirProfil?.DepartmanId // departman amirden gelir
            });
            await db.SaveChangesAsync();
            yeniOlusan = true;
        }

        if (yeniOlusan)
            await denetim.KaydetAsync("Sistem", "Faz1SeedTamamlandi", "Departmanlar ve örnek profiller oluşturuldu.");
    }
}
