using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    }
}
