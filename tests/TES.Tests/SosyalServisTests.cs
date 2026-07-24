using TES.Domain.Entities;
using TES.Infrastructure.Services;

namespace TES.Tests;

/// <summary>
/// Sosyal platform kuralları (CLAUDE.md Bölüm 5, 13): stajyer gönderisi onaysız yayına
/// ALINMAZ; amir/admin gönderisi onaydan muaf; içerik kaldırma sahiplik/rol ile denetlenir.
/// </summary>
public class SosyalServisTests : IDisposable
{
    private readonly TestVeritabani _veritabani = new();
    private readonly SosyalServis _servis;

    private readonly string _adminId;
    private readonly string _amirId;
    private readonly string _stajyerId;
    private readonly string _baskaStajyerId;

    public SosyalServisTests()
    {
        _servis = new SosyalServis(_veritabani.Db, new DenetimServisi(_veritabani.Db));

        _adminId = _veritabani.KullaniciEkle("admin", "Sistem Yöneticisi");
        _amirId = _veritabani.KullaniciEkle("amir", "Amir Kişi");
        _stajyerId = _veritabani.KullaniciEkle("stajyer", "Stajyer Kişi");
        _baskaStajyerId = _veritabani.KullaniciEkle("stajyer2", "Başka Stajyer");
    }

    private YetkiBaglami Admin => new(_adminId, true, false, false);
    private YetkiBaglami Amir => new(_amirId, false, true, false);
    private YetkiBaglami Stajyer => new(_stajyerId, false, false, true);
    private YetkiBaglami BaskaStajyer => new(_baskaStajyerId, false, false, true);

    [Fact]
    public async Task StajyerGonderisi_Beklemede_YayinaAlinmaz()
    {
        var durum = await _servis.GonderiOlusturAsync("Merhaba", Stajyer);

        Assert.Equal(ModerasyonDurumu.Beklemede, durum);
        Assert.Equal(ModerasyonDurumu.Beklemede, _veritabani.Db.Gonderiler.Single().ModerasyonDurumu);
    }

    [Theory]
    [InlineData(false)] // amir
    [InlineData(true)]  // admin
    public async Task AmirVeyaAdminGonderisi_OnaydanMuaf(bool admin)
    {
        var yetki = admin ? Admin : Amir;

        var durum = await _servis.GonderiOlusturAsync("Duyuru", yetki);

        Assert.Equal(ModerasyonDurumu.Onaylandi, durum);
    }

    [Fact]
    public async Task BekleyenStajyerGonderisi_BaskasininFeedindeGorunmez_SahibineGorunur()
    {
        await _servis.GonderiOlusturAsync("Taslak paylaşım", Stajyer);

        // Sahibine görünür (durum etiketiyle).
        Assert.Single(await _servis.FeedGetirAsync(Stajyer));

        // Başka stajyere görünmez (henüz onaylanmadı).
        Assert.Empty(await _servis.FeedGetirAsync(BaskaStajyer));
    }

    [Fact]
    public async Task Onaylandiktan_Sonra_HerkeseGorunur()
    {
        await _servis.GonderiOlusturAsync("Onay bekleyen", Stajyer);
        var gonderiId = _veritabani.Db.Gonderiler.Single().Id;

        await _servis.OnaylaAsync(gonderiId, "admin");

        Assert.Single(await _servis.FeedGetirAsync(BaskaStajyer));
    }

    [Fact]
    public async Task Reddedilen_RedMesajiKaydedilir_SahibineGorunur()
    {
        await _servis.GonderiOlusturAsync("Uygunsuz?", Stajyer);
        var gonderiId = _veritabani.Db.Gonderiler.Single().Id;

        var sonuc = await _servis.ReddetAsync(gonderiId, "Kurum diline uygun değil.", "admin");

        Assert.True(sonuc.Basarili);
        var g = _veritabani.Db.Gonderiler.Single();
        Assert.Equal(ModerasyonDurumu.Reddedildi, g.ModerasyonDurumu);
        Assert.Equal("Kurum diline uygun değil.", g.RedMesaji);
    }

    [Fact]
    public async Task Reddet_BosGerekce_Reddedilir()
    {
        await _servis.GonderiOlusturAsync("x", Stajyer);
        var gonderiId = _veritabani.Db.Gonderiler.Single().Id;

        var sonuc = await _servis.ReddetAsync(gonderiId, "   ", "admin");

        Assert.False(sonuc.Basarili);
    }

    [Fact]
    public async Task Gonderi_SahibiSilebilir_BaskasiSilemez_AdminHepsiniSilebilir()
    {
        await _servis.GonderiOlusturAsync("Amir duyurusu", Amir);
        var gonderiId = _veritabani.Db.Gonderiler.Single().Id;

        // Başka bir kullanıcı (stajyer) silemez.
        Assert.False((await _servis.GonderiSilAsync(gonderiId, Stajyer)).Basarili);
        Assert.Single(_veritabani.Db.Gonderiler);

        // Admin herkesin gönderisini silebilir.
        Assert.True((await _servis.GonderiSilAsync(gonderiId, Admin)).Basarili);
        Assert.Empty(_veritabani.Db.Gonderiler);
    }

    [Fact]
    public async Task Yorum_YalnizcaOnayliGonderiye_Eklenebilir()
    {
        await _servis.GonderiOlusturAsync("Bekleyen", Stajyer);
        var gonderiId = _veritabani.Db.Gonderiler.Single().Id;

        // Bekleyen gönderiye yorum yapılamaz.
        Assert.False((await _servis.YorumEkleAsync(gonderiId, "yorum", Amir)).Basarili);

        // Onaylanınca yorum eklenebilir.
        await _servis.OnaylaAsync(gonderiId, "admin");
        Assert.True((await _servis.YorumEkleAsync(gonderiId, "güzel", Amir)).Basarili);
        Assert.Single(_veritabani.Db.Yorumlar);
    }

    [Fact]
    public async Task Yorum_SahibiVeyaAdminSilebilir()
    {
        await _servis.GonderiOlusturAsync("Duyuru", Amir);
        var gonderiId = _veritabani.Db.Gonderiler.Single().Id;
        await _servis.YorumEkleAsync(gonderiId, "stajyer yorumu", Stajyer);
        var yorumId = _veritabani.Db.Yorumlar.Single().Id;

        // Başka stajyer silemez.
        Assert.False((await _servis.YorumSilAsync(yorumId, BaskaStajyer)).Basarili);
        // Admin silebilir.
        Assert.True((await _servis.YorumSilAsync(yorumId, Admin)).Basarili);
        Assert.Empty(_veritabani.Db.Yorumlar);
    }

    [Fact]
    public async Task Begeni_ToggleCalisir_AyniKullaniciTekBegeni()
    {
        await _servis.GonderiOlusturAsync("Beğenilesi", Amir);
        var gonderiId = _veritabani.Db.Gonderiler.Single().Id;

        var (b1, begenildi1, _) = await _servis.BegeniToggleAsync(gonderiId, Stajyer);
        Assert.True(b1);
        Assert.True(begenildi1);
        Assert.Single(_veritabani.Db.Begeniler);

        // İkinci toggle beğeniyi kaldırır.
        var (b2, begenildi2, _) = await _servis.BegeniToggleAsync(gonderiId, Stajyer);
        Assert.True(b2);
        Assert.False(begenildi2);
        Assert.Empty(_veritabani.Db.Begeniler);
    }

    [Fact]
    public async Task Begeni_BekleyenGonderiye_Yapilamaz()
    {
        await _servis.GonderiOlusturAsync("Bekleyen", Stajyer);
        var gonderiId = _veritabani.Db.Gonderiler.Single().Id;

        var (basarili, _, _) = await _servis.BegeniToggleAsync(gonderiId, Amir);

        Assert.False(basarili);
        Assert.Empty(_veritabani.Db.Begeniler);
    }

    public void Dispose() => _veritabani.Dispose();
}
