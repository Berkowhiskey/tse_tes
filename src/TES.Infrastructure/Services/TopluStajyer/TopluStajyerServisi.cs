using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TES.Domain.Entities;
using TES.Domain.Kurallar;
using TES.Domain.Sabitler;
using TES.Infrastructure.Data;
using TES.Infrastructure.Identity;

namespace TES.Infrastructure.Services;

/// <summary>
/// Toplu stajyer içe aktarma. Tekil akışın kurallarını korur (KullaniciAdiUretici, KartNo tekilliği,
/// T.C.=geçici parola, ForceChangePassword) ama satır satır <c>StajyerOlusturAsync</c> çağırmak yerine
/// dedike, tek transaction'lı bir döngü kullanır: kullanıcı adı havuzu batch boyunca güncel tutulur
/// (aksi halde aynı dosyadaki iki aynı isim çakışırdı). T.C. yalnızca CreateAsync'e verilir; SAKLANMAZ/loglanmaz.
/// </summary>
public class TopluStajyerServisi(
    TesDbContext db,
    UserManager<Kullanici> userManager,
    IDenetimServisi denetim) : ITopluStajyerServisi
{
    private const int MaksSatir = 1000; // kötüye kullanım / kaza sınırı

    public byte[] SablonUret() => StajyerSablonu.Uret();

    public async Task<TopluIceAktarmaSonucu> IceAktarAsync(Stream xlsx, string aktor)
    {
        List<TopluStajyerSatir> satirlar;
        try
        {
            satirlar = SatirlariOku(xlsx);
        }
        catch (Exception)
        {
            // İçerik loglanmaz — yalnızca genel hata.
            return TopluIceAktarmaSonucu.DosyaHatasi("Dosya okunamadı: geçerli bir .xlsx dosyası değil.");
        }

        if (satirlar.Count == 0)
            return TopluIceAktarmaSonucu.DosyaHatasi("Dosyada veri satırı bulunamadı (yalnız başlık?).");

        if (satirlar.Count > MaksSatir)
            return TopluIceAktarmaSonucu.DosyaHatasi($"Satır sınırı aşıldı (en fazla {MaksSatir}).");

        var mevcutKullaniciAdlari = (await db.Users.Select(u => u.UserName!).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mevcutKartNolar = (await db.StajyerProfilleri.Select(s => s.KartNo).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var (gecerliler, hatalar) = TopluStajyerDogrulayici.Dogrula(satirlar, mevcutKartNolar);
        var tumHatalar = new List<SatirHatasi>(hatalar);
        var eklenenAdlar = new List<string>();

        if (gecerliler.Count > 0)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            foreach (var stajyer in gecerliler)
            {
                string kullaniciAdi;
                try
                {
                    kullaniciAdi = KullaniciAdiUretici.UretBenzersiz(
                        stajyer.Ad, stajyer.Soyad, stajyer.KartNo, mevcutKullaniciAdlari);
                }
                catch (ArgumentException)
                {
                    tumHatalar.Add(new SatirHatasi(stajyer.SatirNo, "Ad/Soyaddan geçerli kullanıcı adı üretilemedi."));
                    continue;
                }

                var kullanici = new Kullanici
                {
                    UserName = kullaniciAdi,
                    AdSoyad = $"{stajyer.Ad} {stajyer.Soyad}",
                    ForceChangePassword = true
                };

                var sonuc = await userManager.CreateAsync(kullanici, stajyer.TcKimlikNo);
                if (!sonuc.Succeeded)
                {
                    tumHatalar.Add(new SatirHatasi(stajyer.SatirNo,
                        string.Join("; ", sonuc.Errors.Select(HataMesajiCevir))));
                    continue;
                }

                await userManager.AddToRoleAsync(kullanici, Roller.Stajyer);

                db.StajyerProfilleri.Add(new StajyerProfil
                {
                    KullaniciId = kullanici.Id,
                    KartNo = stajyer.KartNo,
                    StajBaslangic = stajyer.StajBaslangic,
                    StajBitis = stajyer.StajBitis,
                    Okul = stajyer.Okul,
                    Bolum = stajyer.Bolum,
                    AmirId = null,      // amir sonradan "Eşleştir" ile atanır
                    DepartmanId = null  // departman amirden gelir
                });

                // Batch-içi çakışmayı önle: üretilen ad/kart havuza eklenir.
                mevcutKullaniciAdlari.Add(kullaniciAdi);
                mevcutKartNolar.Add(stajyer.KartNo);
                eklenenAdlar.Add(kullaniciAdi);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await denetim.KaydetAsync(aktor, "TopluStajyerIceAktarildi",
            $"Eklenen: {eklenenAdlar.Count}, Atlanan: {tumHatalar.Count(h => h.SatirNo > 0)}");

        // Hataları satır numarasına göre sırala (rapor okunurluğu).
        var siraliHatalar = tumHatalar.OrderBy(h => h.SatirNo).ToList();
        return new TopluIceAktarmaSonucu(eklenenAdlar.Count, siraliHatalar, eklenenAdlar);
    }

    /// <summary>Çalışma sayfasını satır listesine çevirir. Boş satırlar atlanır.</summary>
    private static List<TopluStajyerSatir> SatirlariOku(Stream xlsx)
    {
        using var workbook = new XLWorkbook(xlsx);
        var sayfa = workbook.Worksheets.TryGetWorksheet(StajyerSablonu.SayfaAdi, out var bulunan)
            ? bulunan
            : workbook.Worksheet(1);

        var satirlar = new List<TopluStajyerSatir>();
        var sonSatir = sayfa.LastRowUsed()?.RowNumber() ?? 1;

        for (var r = 2; r <= sonSatir; r++) // 1. satır başlık
        {
            var satir = sayfa.Row(r);
            if (satir.IsEmpty())
                continue;

            var (basMetin, basTarih) = TarihOku(satir.Cell(5));
            var (bitMetin, bitTarih) = TarihOku(satir.Cell(6));

            var oku = new TopluStajyerSatir(
                SatirNo: r,
                Ad: Metin(satir.Cell(1)),
                Soyad: Metin(satir.Cell(2)),
                TcKimlikNo: Metin(satir.Cell(3)),
                KartNo: Metin(satir.Cell(4)),
                StajBaslangicMetin: basMetin,
                StajBaslangic: basTarih,
                StajBitisMetin: bitMetin,
                StajBitis: bitTarih,
                Okul: Metin(satir.Cell(7)),
                Bolum: Metin(satir.Cell(8)));

            // Tamamen boş (tüm alanlar null) satırı atla.
            if (oku is { Ad: null, Soyad: null, TcKimlikNo: null, KartNo: null,
                         StajBaslangicMetin: null, StajBitisMetin: null, Okul: null, Bolum: null })
                continue;

            satirlar.Add(oku);
        }

        return satirlar;
    }

    private static string? Metin(IXLCell hucre)
    {
        if (hucre.IsEmpty())
            return null;

        // Sayısal hücreler (ör. T.C./Kart no Excel'de sayı olabilir) tam sayı olarak metne çevrilir.
        if (hucre.DataType == XLDataType.Number)
        {
            var sayi = hucre.GetDouble();
            if (sayi == Math.Floor(sayi) && !double.IsInfinity(sayi))
                return ((long)sayi).ToString(CultureInfo.InvariantCulture);
        }

        var s = hucre.GetString().Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static (string? Metin, DateOnly? Tarih) TarihOku(IXLCell hucre)
    {
        if (hucre.IsEmpty())
            return (null, null);

        if (hucre.DataType == XLDataType.DateTime)
        {
            var dt = hucre.GetDateTime();
            return (dt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), DateOnly.FromDateTime(dt));
        }

        var s = hucre.GetString().Trim();
        if (string.IsNullOrEmpty(s))
            return (null, null);

        return (s, TarihAyristir(s));
    }

    private static readonly string[] TarihBicimleri =
        ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy"];

    private static DateOnly? TarihAyristir(string metin) =>
        DateOnly.TryParseExact(metin, TarihBicimleri, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
            ? t
            : null;

    private static string HataMesajiCevir(IdentityError hata) => hata.Code switch
    {
        "PasswordTooShort" => "T.C. Kimlik No 11 haneli olmalıdır.",
        "PasswordRequiresDigit" => "T.C. Kimlik No yalnızca rakamlardan oluşmalıdır.",
        "DuplicateUserName" => "Kullanıcı adı zaten mevcut.",
        _ => hata.Description
    };
}
