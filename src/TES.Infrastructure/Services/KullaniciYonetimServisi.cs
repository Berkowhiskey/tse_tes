using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TES.Domain.Entities;
using TES.Domain.Kurallar;
using TES.Domain.Sabitler;
using TES.Infrastructure.Data;
using TES.Infrastructure.Identity;

namespace TES.Infrastructure.Services;

/// <summary>
/// Admin'in amir/stajyer hesabı açması ve amir–stajyer eşleştirmesi.
/// Kural (CLAUDE.md Bölüm 7): ilk geçici parola = T.C. Kimlik No (yalnızca hash'lenir,
/// saklanmaz/loglanmaz) ve ilk girişte parola değişimi zorunludur.
/// </summary>
public class KullaniciYonetimServisi(
    TesDbContext db,
    UserManager<Kullanici> userManager,
    IDenetimServisi denetim) : IKullaniciYonetimServisi
{
    public async Task<IReadOnlyList<AmirSatiri>> AmirleriListeleAsync()
    {
        var veri = await db.AmirProfilleri
            .AsNoTracking()
            .Select(a => new
            {
                a.Id,
                Kullanici = db.Users.First(u => u.Id == a.KullaniciId),
                DepartmanAd = a.Departman!.Ad,
                StajyerSayisi = a.Stajyerler.Count
            })
            .ToListAsync();

        return veri
            .Select(x => new AmirSatiri(x.Id, x.Kullanici.UserName ?? "?", x.Kullanici.AdSoyad, x.DepartmanAd, x.StajyerSayisi))
            .OrderBy(x => x.AdSoyad)
            .ToList();
    }

    public async Task<KullaniciOlusturmaSonucu> AmirOlusturAsync(YeniAmirBilgisi bilgi, string aktor)
    {
        if (!await db.Departmanlar.AnyAsync(d => d.Id == bilgi.DepartmanId))
            return KullaniciOlusturmaSonucu.Hata("Seçilen departman bulunamadı.");

        var (kullanici, hatalar) = await KullaniciOlusturAsync(bilgi.Ad, bilgi.Soyad, kartNo: null, bilgi.TcKimlikNo, Roller.Amir);
        if (kullanici is null)
            return KullaniciOlusturmaSonucu.Hata([.. hatalar]);

        db.AmirProfilleri.Add(new AmirProfil
        {
            KullaniciId = kullanici.Id,
            DepartmanId = bilgi.DepartmanId,
            IseBaslamaTarihi = bilgi.IseBaslamaTarihi,
            Hakkimda = bilgi.Hakkimda
        });
        await db.SaveChangesAsync();

        await denetim.KaydetAsync(aktor, "AmirOlusturuldu", $"Kullanıcı: {kullanici.UserName}");
        return KullaniciOlusturmaSonucu.Basari(kullanici.UserName!);
    }

    public async Task<KullaniciOlusturmaSonucu> StajyerOlusturAsync(YeniStajyerBilgisi bilgi, string aktor)
    {
        if (await db.StajyerProfilleri.AnyAsync(s => s.KartNo == bilgi.KartNo))
            return KullaniciOlusturmaSonucu.Hata("Bu kart numarası başka bir stajyere atanmış.");

        if (bilgi.StajBitis <= bilgi.StajBaslangic)
            return KullaniciOlusturmaSonucu.Hata("Staj bitişi, başlangıcından sonra olmalıdır.");

        AmirProfil? amir = null;
        if (bilgi.AmirProfilId is not null)
        {
            amir = await db.AmirProfilleri.FirstOrDefaultAsync(a => a.Id == bilgi.AmirProfilId);
            if (amir is null)
                return KullaniciOlusturmaSonucu.Hata("Seçilen amir bulunamadı.");
        }

        var (kullanici, hatalar) = await KullaniciOlusturAsync(bilgi.Ad, bilgi.Soyad, bilgi.KartNo, bilgi.TcKimlikNo, Roller.Stajyer);
        if (kullanici is null)
            return KullaniciOlusturmaSonucu.Hata([.. hatalar]);

        db.StajyerProfilleri.Add(new StajyerProfil
        {
            KullaniciId = kullanici.Id,
            KartNo = bilgi.KartNo,
            StajBaslangic = bilgi.StajBaslangic,
            StajBitis = bilgi.StajBitis,
            Okul = bilgi.Okul,
            Bolum = bilgi.Bolum,
            AmirId = amir?.Id,
            DepartmanId = amir?.DepartmanId // departman amirden gelir
        });
        await db.SaveChangesAsync();

        await denetim.KaydetAsync(aktor, "StajyerOlusturuldu",
            $"Kullanıcı: {kullanici.UserName}, Amir: {(amir is null ? "atanmadı" : amir.Id.ToString())}");
        return KullaniciOlusturmaSonucu.Basari(kullanici.UserName!);
    }

    public async Task<(bool Basarili, string? Hata)> StajyerAmireAtaAsync(int stajyerProfilId, int? amirProfilId, string aktor)
    {
        var stajyer = await db.StajyerProfilleri.FirstOrDefaultAsync(s => s.Id == stajyerProfilId);
        if (stajyer is null)
            return (false, "Stajyer bulunamadı.");

        AmirProfil? amir = null;
        if (amirProfilId is not null)
        {
            amir = await db.AmirProfilleri.FirstOrDefaultAsync(a => a.Id == amirProfilId);
            if (amir is null)
                return (false, "Amir bulunamadı.");
        }

        stajyer.AmirId = amir?.Id;
        stajyer.DepartmanId = amir?.DepartmanId; // departman amirden gelir
        await db.SaveChangesAsync();

        await denetim.KaydetAsync(aktor, "EslestirmeDegisti",
            $"StajyerProfil: {stajyerProfilId}, Amir: {(amir is null ? "kaldırıldı" : amir.Id.ToString())}");
        return (true, null);
    }

    /// <summary>
    /// Identity kullanıcısını oluşturur. Geçici parola (T.C.) yalnızca CreateAsync'e verilir;
    /// hash'lenir, hiçbir yerde saklanmaz veya loglanmaz.
    /// </summary>
    private async Task<(Kullanici? Kullanici, IEnumerable<string> Hatalar)> KullaniciOlusturAsync(
        string ad, string soyad, string? kartNo, string geciciParolaTc, string rol)
    {
        var mevcutAdlar = (await db.Users.Select(u => u.UserName!).ToListAsync()).ToHashSet();
        var kullaniciAdi = KullaniciAdiUretici.UretBenzersiz(ad, soyad, kartNo, mevcutAdlar);

        var kullanici = new Kullanici
        {
            UserName = kullaniciAdi,
            AdSoyad = $"{ad.Trim()} {soyad.Trim()}",
            ForceChangePassword = true // ilk girişte parola değişimi zorunlu
        };

        var sonuc = await userManager.CreateAsync(kullanici, geciciParolaTc);
        if (!sonuc.Succeeded)
            return (null, sonuc.Errors.Select(HataMesajiCevir));

        await userManager.AddToRoleAsync(kullanici, rol);
        return (kullanici, []);
    }

    private static string HataMesajiCevir(IdentityError hata) => hata.Code switch
    {
        "PasswordTooShort" => "T.C. Kimlik No 11 haneli olmalıdır.",
        "PasswordRequiresDigit" => "T.C. Kimlik No yalnızca rakamlardan oluşmalıdır.",
        _ => hata.Description
    };
}
