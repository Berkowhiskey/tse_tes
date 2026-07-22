using System.Text;

namespace TES.Domain.Kurallar;

/// <summary>
/// Kullanıcı adı üretim kuralı: <c>ad_soyad_kartno</c>.
/// Türkçe karakterler ASCII'ye normalize edilir, boşluklar ve isim çakışmaları
/// deterministik biçimde çözülür (CLAUDE.md Bölüm 7).
/// </summary>
public static class KullaniciAdiUretici
{
    /// <summary>
    /// Ad, soyad ve kart numarasından normalize kullanıcı adı üretir.
    /// Örn: ("Ayşe Gül", "Çelik-Öztürk", "1001") → "ayse_gul_celik_ozturk_1001".
    /// </summary>
    public static string Uret(string ad, string soyad, string? kartNo = null)
    {
        var parcalar = new List<string>
        {
            Normalize(ad),
            Normalize(soyad)
        };

        if (!string.IsNullOrWhiteSpace(kartNo))
            parcalar.Add(Normalize(kartNo));

        var sonuc = string.Join('_', parcalar.Where(p => p.Length > 0));

        if (sonuc.Length == 0)
            throw new ArgumentException("Ad ve soyaddan geçerli bir kullanıcı adı üretilemedi.");

        return sonuc;
    }

    /// <summary>
    /// Mevcut kullanıcı adlarıyla çakışmayı deterministik çözer:
    /// "ayse_yilmaz_1001" doluysa "ayse_yilmaz_1001_2", "ayse_yilmaz_1001_3"...
    /// </summary>
    public static string UretBenzersiz(string ad, string soyad, string? kartNo, IReadOnlySet<string> mevcutAdlar)
    {
        var taban = Uret(ad, soyad, kartNo);

        if (!mevcutAdlar.Contains(taban))
            return taban;

        for (var i = 2; ; i++)
        {
            var aday = $"{taban}_{i}";
            if (!mevcutAdlar.Contains(aday))
                return aday;
        }
    }

    /// <summary>
    /// Metni küçük harfe çevirir, Türkçe karakterleri ASCII'ye dönüştürür,
    /// harf/rakam dışındaki her şeyi tek alt çizgiye indirger.
    /// </summary>
    private static string Normalize(string metin)
    {
        var sb = new StringBuilder(metin.Length);
        var oncekiAltCizgi = true; // baştaki ayraçları at

        foreach (var karakter in metin.Trim())
        {
            var c = KarakterDonustur(char.ToLowerInvariant(TurkceKucult(karakter)));

            if (char.IsAsciiLetterOrDigit(c))
            {
                sb.Append(c);
                oncekiAltCizgi = false;
            }
            else if (!oncekiAltCizgi)
            {
                sb.Append('_');
                oncekiAltCizgi = true;
            }
        }

        // sondaki alt çizgiyi temizle
        while (sb.Length > 0 && sb[^1] == '_')
            sb.Length--;

        return sb.ToString();
    }

    /// <summary>'İ' → 'i', 'I' → 'ı' — invariant ToLower Türkçe I/İ ayrımını bilmez, elle çözülür.</summary>
    private static char TurkceKucult(char c) => c switch
    {
        'İ' => 'i',
        'I' => 'ı',
        _ => c
    };

    private static char KarakterDonustur(char c) => c switch
    {
        'ç' => 'c',
        'ğ' => 'g',
        'ı' => 'i',
        'ö' => 'o',
        'ş' => 's',
        'ü' => 'u',
        _ => c
    };
}
