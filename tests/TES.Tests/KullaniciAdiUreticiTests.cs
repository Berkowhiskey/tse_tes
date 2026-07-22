using TES.Domain.Kurallar;

namespace TES.Tests;

public class KullaniciAdiUreticiTests
{
    [Theory]
    [InlineData("Ayşe", "Yılmaz", "1001", "ayse_yilmaz_1001")]
    [InlineData("Çağrı", "Öztürk", "2002", "cagri_ozturk_2002")]
    [InlineData("İsmail", "Şahin", "3003", "ismail_sahin_3003")]
    [InlineData("Işıl", "Uğur", "4004", "isil_ugur_4004")]
    public void Uret_TurkceKarakterleriAsciiyeNormalizeEder(string ad, string soyad, string kartNo, string beklenen)
    {
        Assert.Equal(beklenen, KullaniciAdiUretici.Uret(ad, soyad, kartNo));
    }

    [Fact]
    public void Uret_BoslukluAdlariAltCizgiyleBirlestirir()
    {
        // Çift ad ve tireli soyad deterministik biçimde çözülür.
        Assert.Equal("ayse_gul_celik_ozturk_1001",
            KullaniciAdiUretici.Uret("Ayşe Gül", "Çelik-Öztürk", "1001"));
    }

    [Fact]
    public void Uret_FazlaBosluklariTekAyracaIndirger()
    {
        Assert.Equal("ali_veli_kaya_5005",
            KullaniciAdiUretici.Uret("  Ali   Veli ", " Kaya ", "5005"));
    }

    [Fact]
    public void Uret_KartNoOlmadanDaCalisir()
    {
        // Amir gibi kart numarası olmayan kullanıcılar için.
        Assert.Equal("mehmet_demir", KullaniciAdiUretici.Uret("Mehmet", "Demir"));
    }

    [Fact]
    public void Uret_BuyukIVeNoktaliIDogruKucultulur()
    {
        // 'I' → 'ı' → 'i' (ASCII), 'İ' → 'i'; invariant kültür farkı elle çözülür.
        Assert.Equal("irmak_ilkin", KullaniciAdiUretici.Uret("IRMAK", "İLKİN"));
    }

    [Fact]
    public void Uret_GecerliKarakterYoksaHataFirlatir()
    {
        Assert.Throws<ArgumentException>(() => KullaniciAdiUretici.Uret("---", "..."));
    }

    [Fact]
    public void UretBenzersiz_CakismaYoksaTabanAdiDondurur()
    {
        var mevcut = new HashSet<string> { "baska_kullanici_1" };

        Assert.Equal("ayse_yilmaz_1001",
            KullaniciAdiUretici.UretBenzersiz("Ayşe", "Yılmaz", "1001", mevcut));
    }

    [Fact]
    public void UretBenzersiz_CakismayiDeterministikSayiEkiyleCozer()
    {
        var mevcut = new HashSet<string> { "ayse_yilmaz_1001", "ayse_yilmaz_1001_2" };

        Assert.Equal("ayse_yilmaz_1001_3",
            KullaniciAdiUretici.UretBenzersiz("Ayşe", "Yılmaz", "1001", mevcut));
    }
}
