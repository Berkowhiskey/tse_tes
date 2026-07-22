using TES.Domain.Entities;
using TES.Infrastructure.Services;

namespace TES.Tests;

/// <summary>
/// Yoklama kuralı (CLAUDE.md Bölüm 6): aynı gün içindeki giriş-çıkış çiftleri eşleştirilir;
/// eşi olmayan giriş "açık oturum" sayılır.
/// </summary>
public class YoklamaServisiTests : IDisposable
{
    private readonly TestVeritabani _veritabani = new();
    private readonly YoklamaServisi _servis;
    private readonly StajyerProfil _stajyer;

    public YoklamaServisiTests()
    {
        _servis = new YoklamaServisi(_veritabani.Db);

        var kullaniciId = _veritabani.KullaniciEkle("test_stajyer_1001", "Test Stajyer");
        _stajyer = new StajyerProfil
        {
            KullaniciId = kullaniciId,
            KartNo = "1001",
            StajBaslangic = new DateOnly(2026, 7, 1),
            StajBitis = new DateOnly(2026, 8, 28)
        };
        _veritabani.Db.StajyerProfilleri.Add(_stajyer);
        _veritabani.Db.SaveChanges();
    }

    [Fact]
    public async Task TaninmayanKart_KartBulunamadiDoner()
    {
        var (sonuc, stajyer) = await _servis.KartOkutAsync("9999");

        Assert.Equal(KartOkutmaSonucu.KartBulunamadi, sonuc);
        Assert.Null(stajyer);
    }

    [Fact]
    public async Task IlkOkutma_GirisAcar()
    {
        var zaman = new DateTime(2026, 7, 22, 8, 30, 0);

        var (sonuc, _) = await _servis.KartOkutAsync("1001", zaman);

        Assert.Equal(KartOkutmaSonucu.GirisYapildi, sonuc);
        var kayit = Assert.Single(_veritabani.Db.YoklamaKayitlari);
        Assert.Equal(zaman, kayit.GirisZamani);
        Assert.Null(kayit.CikisZamani); // açık oturum
    }

    [Fact]
    public async Task AyniGunIkinciOkutma_CikisYazarVeCiftiEslestirir()
    {
        var giris = new DateTime(2026, 7, 22, 8, 30, 0);
        var cikis = new DateTime(2026, 7, 22, 17, 15, 0);

        await _servis.KartOkutAsync("1001", giris);
        var (sonuc, _) = await _servis.KartOkutAsync("1001", cikis);

        Assert.Equal(KartOkutmaSonucu.CikisYapildi, sonuc);
        var kayit = Assert.Single(_veritabani.Db.YoklamaKayitlari);
        Assert.Equal(giris, kayit.GirisZamani);
        Assert.Equal(cikis, kayit.CikisZamani);
    }

    [Fact]
    public async Task AyniGunUcuncuOkutma_YeniGirisAcar()
    {
        var gun = new DateTime(2026, 7, 22, 0, 0, 0);

        await _servis.KartOkutAsync("1001", gun.AddHours(8));   // giriş
        await _servis.KartOkutAsync("1001", gun.AddHours(12));  // çıkış (öğle)
        var (sonuc, _) = await _servis.KartOkutAsync("1001", gun.AddHours(13)); // yeni giriş

        Assert.Equal(KartOkutmaSonucu.GirisYapildi, sonuc);
        Assert.Equal(2, _veritabani.Db.YoklamaKayitlari.Count());
    }

    [Fact]
    public async Task OncekiGununAcikOturumu_BugunkuOkutmayiEtkilemez()
    {
        // Dün çıkış unutuldu → açık oturum. Bugünkü okutma çıkış DEĞİL, yeni giriş olmalı.
        var dun = new DateTime(2026, 7, 21, 8, 0, 0);
        var bugun = new DateTime(2026, 7, 22, 8, 30, 0);

        await _servis.KartOkutAsync("1001", dun);
        var (sonuc, _) = await _servis.KartOkutAsync("1001", bugun);

        Assert.Equal(KartOkutmaSonucu.GirisYapildi, sonuc);
        Assert.Equal(2, _veritabani.Db.YoklamaKayitlari.Count());

        var dununKaydi = _veritabani.Db.YoklamaKayitlari.Single(y => y.GirisZamani == dun);
        Assert.Null(dununKaydi.CikisZamani); // açık oturum korunur
    }

    public void Dispose() => _veritabani.Dispose();
}
