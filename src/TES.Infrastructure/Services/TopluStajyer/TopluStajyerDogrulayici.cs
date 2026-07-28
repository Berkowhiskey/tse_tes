using System.Text.RegularExpressions;

namespace TES.Infrastructure.Services;

/// <summary>
/// Toplu içe aktarma satır doğrulaması — SAF (DB'siz, UserManager'sız): kolayca birim test edilir.
/// Kurallar tekil ekleme akışıyla aynıdır: zorunlu alanlar, T.C. 11 hane, Bitiş &gt; Başlangıç,
/// benzersiz Kart No (hem dosya içinde hem mevcut DB'ye karşı). T.C. burada SAKLANMAZ/loglanmaz.
/// </summary>
public static partial class TopluStajyerDogrulayici
{
    [GeneratedRegex(@"^\d{11}$")]
    private static partial Regex TcDeseni();

    /// <summary>
    /// Satırları doğrular. <paramref name="mevcutKartNolar"/> DB'de zaten kayıtlı kart numaralarıdır.
    /// Geçerli satırları ve (SatirNo, Neden) hata listesini döndürür. Her hatalı satır tek kayıt üretir
    /// (birden çok neden "; " ile birleştirilir).
    /// </summary>
    public static (IReadOnlyList<GecerliStajyer> Gecerliler, IReadOnlyList<SatirHatasi> Hatalar) Dogrula(
        IReadOnlyList<TopluStajyerSatir> satirlar,
        IReadOnlySet<string> mevcutKartNolar)
    {
        var gecerliler = new List<GecerliStajyer>();
        var hatalar = new List<SatirHatasi>();
        var dosyadakiKartNolar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var satir in satirlar)
        {
            var nedenler = new List<string>();

            var ad = satir.Ad?.Trim();
            var soyad = satir.Soyad?.Trim();
            var tc = satir.TcKimlikNo?.Trim();
            var kartNo = satir.KartNo?.Trim();

            if (string.IsNullOrWhiteSpace(ad)) nedenler.Add("Ad boş");
            if (string.IsNullOrWhiteSpace(soyad)) nedenler.Add("Soyad boş");

            if (string.IsNullOrWhiteSpace(tc))
                nedenler.Add("T.C. Kimlik No boş");
            else if (!TcDeseni().IsMatch(tc))
                nedenler.Add("T.C. Kimlik No 11 haneli rakam olmalı");

            if (string.IsNullOrWhiteSpace(kartNo))
            {
                nedenler.Add("Kart No boş");
            }
            else if (!dosyadakiKartNolar.Add(kartNo))
            {
                nedenler.Add("Kart No dosyada tekrar ediyor");
            }
            else if (mevcutKartNolar.Contains(kartNo))
            {
                nedenler.Add("Kart No zaten başka bir stajyere atanmış");
            }

            // Tarihler: boş mu, geçersiz format mı ayrımı ham metinle yapılır.
            if (satir.StajBaslangic is null)
                nedenler.Add(string.IsNullOrWhiteSpace(satir.StajBaslangicMetin)
                    ? "Staj başlangıcı boş" : "Staj başlangıcı geçersiz tarih (GG.AA.YYYY)");

            if (satir.StajBitis is null)
                nedenler.Add(string.IsNullOrWhiteSpace(satir.StajBitisMetin)
                    ? "Staj bitişi boş" : "Staj bitişi geçersiz tarih (GG.AA.YYYY)");

            if (satir.StajBaslangic is { } bas && satir.StajBitis is { } bit && bit <= bas)
                nedenler.Add("Staj bitişi, başlangıcından sonra olmalı");

            if (nedenler.Count > 0)
            {
                hatalar.Add(new SatirHatasi(satir.SatirNo, string.Join("; ", nedenler)));
                continue;
            }

            gecerliler.Add(new GecerliStajyer(
                satir.SatirNo, ad!, soyad!, tc!, kartNo!,
                satir.StajBaslangic!.Value, satir.StajBitis!.Value,
                string.IsNullOrWhiteSpace(satir.Okul) ? null : satir.Okul!.Trim(),
                string.IsNullOrWhiteSpace(satir.Bolum) ? null : satir.Bolum!.Trim()));
        }

        return (gecerliler, hatalar);
    }
}
