using TES.Domain.Entities;
using TES.Infrastructure.Services;

namespace TES.Tests;

/// <summary>
/// Proje ve Ödev servislerinin sahiplik/yetki kuralları (CLAUDE.md Bölüm 5):
/// amir yalnızca kendi stajyeri, stajyer yalnızca kendi işi üzerinde işlem yapabilir.
/// </summary>
public class IsTakibiServisiTests : IDisposable
{
    private readonly TestVeritabani _veritabani = new();
    private readonly ProjeServisi _proje;
    private readonly OdevServisi _odev;

    private readonly string _amirKullaniciId;
    private readonly string _baskaAmirKullaniciId;
    private readonly string _stajyerKullaniciId;
    private readonly int _amirProfilId;
    private readonly int _stajyerProfilId;

    public IsTakibiServisiTests()
    {
        var denetim = new DenetimServisi(_veritabani.Db);
        _proje = new ProjeServisi(_veritabani.Db, denetim);
        _odev = new OdevServisi(_veritabani.Db, denetim);

        // Departman + iki amir + bir stajyer (amiri: birinci amir).
        var departman = new Departman { Ad = "Yazılım" };
        _veritabani.Db.Departmanlar.Add(departman);
        _veritabani.Db.SaveChanges();

        _amirKullaniciId = _veritabani.KullaniciEkle("amir_bir", "Amir Bir");
        _baskaAmirKullaniciId = _veritabani.KullaniciEkle("amir_iki", "Amir İki");
        _stajyerKullaniciId = _veritabani.KullaniciEkle("stajyer_bir", "Stajyer Bir");

        var amir = new AmirProfil { KullaniciId = _amirKullaniciId, DepartmanId = departman.Id };
        var baskaAmir = new AmirProfil { KullaniciId = _baskaAmirKullaniciId, DepartmanId = departman.Id };
        _veritabani.Db.AmirProfilleri.AddRange(amir, baskaAmir);
        _veritabani.Db.SaveChanges();
        _amirProfilId = amir.Id;

        var stajyer = new StajyerProfil
        {
            KullaniciId = _stajyerKullaniciId,
            KartNo = "5001",
            StajBaslangic = new DateOnly(2026, 7, 1),
            StajBitis = new DateOnly(2026, 8, 28),
            AmirId = amir.Id,
            DepartmanId = departman.Id
        };
        _veritabani.Db.StajyerProfilleri.Add(stajyer);
        _veritabani.Db.SaveChanges();
        _stajyerProfilId = stajyer.Id;
    }

    private YetkiBaglami AmirYetkisi => new(_amirKullaniciId, false, true, false);
    private YetkiBaglami BaskaAmirYetkisi => new(_baskaAmirKullaniciId, false, true, false);
    private YetkiBaglami StajyerYetkisi => new(_stajyerKullaniciId, false, false, true);
    private YetkiBaglami AdminYetkisi => new("admin-id", true, false, false);

    // ---- Proje ----

    [Fact]
    public async Task Proje_KendiStajyerininAmiri_AtayabilirVeTekProjeKuraliKorunur()
    {
        Assert.True((await _proje.KaydetAsync(_stajyerProfilId, "Proje A", "açıklama", AmirYetkisi)).Basarili);

        // İkinci kez kaydetmek yeni proje oluşturmaz, mevcudu günceller (tek proje).
        Assert.True((await _proje.KaydetAsync(_stajyerProfilId, "Proje A2", null, AmirYetkisi)).Basarili);

        Assert.Single(_veritabani.Db.Projeler);
        Assert.Equal("Proje A2", _veritabani.Db.Projeler.Single().Ad);
    }

    [Fact]
    public async Task Proje_BaskaAmir_AtayamazVeReddedilir()
    {
        var sonuc = await _proje.KaydetAsync(_stajyerProfilId, "İzinsiz", null, BaskaAmirYetkisi);

        Assert.False(sonuc.Basarili);
        Assert.Empty(_veritabani.Db.Projeler);
    }

    [Fact]
    public async Task Proje_SahibiStajyer_IlerlemeGuncelleyebilir()
    {
        await _proje.KaydetAsync(_stajyerProfilId, "Proje", null, AmirYetkisi);
        var projeId = _veritabani.Db.Projeler.Single().Id;

        var sonuc = await _proje.IlerlemeGuncelleAsync(projeId, 100, IsDurumu.DevamEdiyor, StajyerYetkisi);

        Assert.True(sonuc.Basarili);
        var proje = _veritabani.Db.Projeler.Single();
        Assert.Equal(100, proje.Ilerleme);
        Assert.Equal(IsDurumu.Tamamlandi, proje.Durum); // %100 → otomatik Tamamlandı
    }

    [Fact]
    public async Task Proje_YabanciStajyer_IlerlemeGuncelleyemez()
    {
        await _proje.KaydetAsync(_stajyerProfilId, "Proje", null, AmirYetkisi);
        var projeId = _veritabani.Db.Projeler.Single().Id;

        var yabanciStajyer = new YetkiBaglami("baska-stajyer-id", false, false, true);
        var sonuc = await _proje.IlerlemeGuncelleAsync(projeId, 50, IsDurumu.DevamEdiyor, yabanciStajyer);

        Assert.False(sonuc.Basarili);
        Assert.Equal(0, _veritabani.Db.Projeler.Single().Ilerleme);
    }

    // ---- Ödev ----

    [Fact]
    public async Task Odev_Amir_KendiStajyerineAtar_AmirProfilIdDogruSetlenir()
    {
        var sonuc = await _odev.AtaAsync(_stajyerProfilId, "Ödev 1", "yap", new DateOnly(2026, 8, 1), AmirYetkisi);

        Assert.True(sonuc.Basarili);
        var odev = Assert.Single(_veritabani.Db.Odevler);
        Assert.Equal(_amirProfilId, odev.AmirProfilId);
    }

    [Fact]
    public async Task Odev_BaskaAmir_Atayamaz()
    {
        var sonuc = await _odev.AtaAsync(_stajyerProfilId, "İzinsiz", null, null, BaskaAmirYetkisi);

        Assert.False(sonuc.Basarili);
        Assert.Empty(_veritabani.Db.Odevler);
    }

    [Fact]
    public async Task Odev_SahibiStajyer_DurumGuncelleyebilir_YabanciGuncelleyemez()
    {
        await _odev.AtaAsync(_stajyerProfilId, "Ödev", null, null, AmirYetkisi);
        var odevId = _veritabani.Db.Odevler.Single().Id;

        // Sahibi stajyer güncelleyebilir.
        Assert.True((await _odev.DurumGuncelleAsync(odevId, 40, IsDurumu.DevamEdiyor, StajyerYetkisi)).Basarili);
        Assert.Equal(40, _veritabani.Db.Odevler.Single().Ilerleme);

        // Yabancı bir stajyer güncelleyemez.
        var yabanci = new YetkiBaglami("baska-stajyer-id", false, false, true);
        Assert.False((await _odev.DurumGuncelleAsync(odevId, 90, IsDurumu.DevamEdiyor, yabanci)).Basarili);
        Assert.Equal(40, _veritabani.Db.Odevler.Single().Ilerleme);
    }

    [Fact]
    public async Task Odev_AmirsizStajyere_AtanamazUyariVerir()
    {
        // Amiri olmayan yeni stajyer
        var kid = _veritabani.KullaniciEkle("stajyer_amirsiz", "Amirsiz Stajyer");
        var amirsiz = new StajyerProfil
        {
            KullaniciId = kid,
            KartNo = "5002",
            StajBaslangic = new DateOnly(2026, 7, 1),
            StajBitis = new DateOnly(2026, 8, 28)
        };
        _veritabani.Db.StajyerProfilleri.Add(amirsiz);
        await _veritabani.Db.SaveChangesAsync();

        var sonuc = await _odev.AtaAsync(amirsiz.Id, "Ödev", null, null, AdminYetkisi);

        Assert.False(sonuc.Basarili);
        Assert.Empty(_veritabani.Db.Odevler);
    }

    [Fact]
    public async Task Odev_Admin_HerStajyereAtayabilir()
    {
        var sonuc = await _odev.AtaAsync(_stajyerProfilId, "Admin Ödevi", null, null, AdminYetkisi);

        Assert.True(sonuc.Basarili);
        Assert.Single(_veritabani.Db.Odevler);
    }

    public void Dispose() => _veritabani.Dispose();
}
