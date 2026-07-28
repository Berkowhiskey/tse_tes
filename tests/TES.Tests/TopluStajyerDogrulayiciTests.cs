using TES.Infrastructure.Services;

namespace TES.Tests;

/// <summary>
/// Saf satır doğrulama kuralları (DB/UserManager yok): zorunlu alanlar, T.C. 11 hane,
/// tarih sırası, dosya-içi ve DB'ye karşı Kart No tekilliği. T.C. burada saklanmaz.
/// </summary>
public class TopluStajyerDogrulayiciTests
{
    private static TopluStajyerSatir Satir(
        int no = 2, string? ad = "Ayse", string? soyad = "Yilmaz", string? tc = "12345678901",
        string? kart = "2001", DateOnly? bas = null, DateOnly? bit = null,
        string? basMetin = "01.07.2026", string? bitMetin = "28.08.2026") =>
        new(no, ad, soyad, tc, kart,
            basMetin, bas ?? new DateOnly(2026, 7, 1),
            bitMetin, bit ?? new DateOnly(2026, 8, 28),
            Okul: null, Bolum: null);

    private static readonly IReadOnlySet<string> BosDb = new HashSet<string>();

    [Fact]
    public void GecerliSatir_Gecerlilere_Eklenir()
    {
        var (gecerliler, hatalar) = TopluStajyerDogrulayici.Dogrula([Satir()], BosDb);

        Assert.Single(gecerliler);
        Assert.Empty(hatalar);
        Assert.Equal("2001", gecerliler[0].KartNo);
    }

    [Fact]
    public void EksikZorunluAlan_Hata()
    {
        var (gecerliler, hatalar) = TopluStajyerDogrulayici.Dogrula([Satir(ad: "  ", soyad: null)], BosDb);

        Assert.Empty(gecerliler);
        var hata = Assert.Single(hatalar);
        Assert.Contains("Ad boş", hata.Neden);
        Assert.Contains("Soyad boş", hata.Neden);
    }

    [Theory]
    [InlineData("1234567890")]   // 10 hane
    [InlineData("123456789012")] // 12 hane
    [InlineData("1234567890a")]  // harf
    public void GecersizTc_Hata(string tc)
    {
        var (gecerliler, hatalar) = TopluStajyerDogrulayici.Dogrula([Satir(tc: tc)], BosDb);

        Assert.Empty(gecerliler);
        Assert.Contains("T.C.", hatalar[0].Neden);
    }

    [Fact]
    public void BitisBaslangictanOnce_Hata()
    {
        var satir = Satir(
            bas: new DateOnly(2026, 8, 1), basMetin: "01.08.2026",
            bit: new DateOnly(2026, 7, 1), bitMetin: "01.07.2026");

        var (gecerliler, hatalar) = TopluStajyerDogrulayici.Dogrula([satir], BosDb);

        Assert.Empty(gecerliler);
        Assert.Contains("sonra olmalı", hatalar[0].Neden);
    }

    [Fact]
    public void GecersizTarihMetni_Bos_Ile_Ayrilir()
    {
        // Başlangıç tamamen boş (metin null, tarih null).
        var bosTarih = new TopluStajyerSatir(2, "Ayse", "Yilmaz", "12345678901", "2001",
            StajBaslangicMetin: null, StajBaslangic: null,
            StajBitisMetin: "28.08.2026", StajBitis: new DateOnly(2026, 8, 28), Okul: null, Bolum: null);

        // Bitiş metni dolu ama ayrıştırılamadı (tarih null).
        var gecersizTarih = new TopluStajyerSatir(3, "Ayse", "Yilmaz", "12345678901", "2002",
            StajBaslangicMetin: "01.07.2026", StajBaslangic: new DateOnly(2026, 7, 1),
            StajBitisMetin: "abc", StajBitis: null, Okul: null, Bolum: null);

        var (_, hatalar) = TopluStajyerDogrulayici.Dogrula([bosTarih, gecersizTarih], BosDb);

        Assert.Contains(hatalar, h => h.SatirNo == 2 && h.Neden.Contains("başlangıcı boş"));
        Assert.Contains(hatalar, h => h.SatirNo == 3 && h.Neden.Contains("geçersiz tarih"));
    }

    [Fact]
    public void DosyaIci_TekrarEdenKartNo_IkinciSatir_Atlanir()
    {
        var s1 = Satir(no: 2, kart: "5000");
        var s2 = Satir(no: 3, kart: "5000");

        var (gecerliler, hatalar) = TopluStajyerDogrulayici.Dogrula([s1, s2], BosDb);

        Assert.Single(gecerliler);           // ilki geçer
        Assert.Equal(2, gecerliler[0].SatirNo);
        var hata = Assert.Single(hatalar);
        Assert.Equal(3, hata.SatirNo);
        Assert.Contains("tekrar ediyor", hata.Neden);
    }

    [Fact]
    public void DbdeMevcutKartNo_Atlanir()
    {
        var mevcut = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1001" };

        var (gecerliler, hatalar) = TopluStajyerDogrulayici.Dogrula([Satir(kart: "1001")], mevcut);

        Assert.Empty(gecerliler);
        Assert.Contains("atanmış", hatalar[0].Neden);
    }
}
