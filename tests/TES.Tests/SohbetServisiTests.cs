using TES.Domain.Entities;
using TES.Infrastructure.Services;

namespace TES.Tests;

/// <summary>
/// Sohbet sahiplik kuralları (CLAUDE.md Bölüm 5, 10): stajyer yalnız amiriyle, amir yalnız
/// kendi stajyeriyle mesajlaşır; admin izler ama gönderemez. Kurallar SohbetServisi'nde sunucuda.
/// </summary>
public class SohbetServisiTests : IDisposable
{
    private readonly TestVeritabani _veritabani = new();
    private readonly SohbetServisi _servis;

    private readonly string _amirId;
    private readonly string _baskaAmirId;
    private readonly string _stajyerId;      // amiri: _amirId
    private readonly string _baskaStajyerId; // amiri: _baskaAmirId

    public SohbetServisiTests()
    {
        _servis = new SohbetServisi(_veritabani.Db);

        var departman = new Departman { Ad = "Yazılım" };
        _veritabani.Db.Departmanlar.Add(departman);
        _veritabani.Db.SaveChanges();

        _amirId = _veritabani.KullaniciEkle("amir_bir", "Amir Bir");
        _baskaAmirId = _veritabani.KullaniciEkle("amir_iki", "Amir İki");
        _stajyerId = _veritabani.KullaniciEkle("stajyer_bir", "Stajyer Bir");
        _baskaStajyerId = _veritabani.KullaniciEkle("stajyer_iki", "Stajyer İki");

        var amir = new AmirProfil { KullaniciId = _amirId, DepartmanId = departman.Id };
        var baskaAmir = new AmirProfil { KullaniciId = _baskaAmirId, DepartmanId = departman.Id };
        _veritabani.Db.AmirProfilleri.AddRange(amir, baskaAmir);
        _veritabani.Db.SaveChanges();

        _veritabani.Db.StajyerProfilleri.AddRange(
            new StajyerProfil
            {
                KullaniciId = _stajyerId, KartNo = "7001", AmirId = amir.Id, DepartmanId = departman.Id,
                StajBaslangic = new DateOnly(2026, 7, 1), StajBitis = new DateOnly(2026, 8, 28)
            },
            new StajyerProfil
            {
                KullaniciId = _baskaStajyerId, KartNo = "7002", AmirId = baskaAmir.Id, DepartmanId = departman.Id,
                StajBaslangic = new DateOnly(2026, 7, 1), StajBitis = new DateOnly(2026, 8, 28)
            });
        _veritabani.Db.SaveChanges();
    }

    [Fact]
    public async Task Amir_KendiStajyeriyle_Konusabilir()
    {
        Assert.True(await _servis.KonusabilirlerMiAsync(_amirId, _stajyerId));
        Assert.True(await _servis.KonusabilirlerMiAsync(_stajyerId, _amirId)); // yön fark etmez
    }

    [Fact]
    public async Task Amir_BaskaStajyerle_Konusamaz()
    {
        Assert.False(await _servis.KonusabilirlerMiAsync(_amirId, _baskaStajyerId));
    }

    [Fact]
    public async Task Stajyer_BaskaStajyerle_Konusamaz()
    {
        Assert.False(await _servis.KonusabilirlerMiAsync(_stajyerId, _baskaStajyerId));
    }

    [Fact]
    public async Task MesajGonder_IzinliCift_Kaydeder()
    {
        var sonuc = await _servis.MesajGonderAsync(_amirId, _stajyerId, "Merhaba");

        Assert.True(sonuc.Basarili);
        Assert.NotNull(sonuc.Mesaj);
        Assert.Single(_veritabani.Db.SohbetMesajlari);
    }

    [Fact]
    public async Task MesajGonder_IzinsizCift_Reddeder()
    {
        var sonuc = await _servis.MesajGonderAsync(_amirId, _baskaStajyerId, "İzinsiz");

        Assert.False(sonuc.Basarili);
        Assert.Empty(_veritabani.Db.SohbetMesajlari);
    }

    [Fact]
    public async Task MesajGonder_BosIcerik_Reddeder()
    {
        var sonuc = await _servis.MesajGonderAsync(_amirId, _stajyerId, "   ");

        Assert.False(sonuc.Basarili);
    }

    [Fact]
    public async Task MesajlariGetir_KarsidanGelenleri_OkunduIsaretler()
    {
        await _servis.MesajGonderAsync(_stajyerId, _amirId, "Soru var");
        Assert.Equal(1, await _servis.OkunmamisToplamAsync(_amirId));

        var amirYetki = new YetkiBaglami(_amirId, false, true, false);
        var (yetkili, mesajlar) = await _servis.MesajlariGetirAsync(_amirId, _stajyerId, amirYetki, okunduIsaretle: true);

        Assert.True(yetkili);
        Assert.Single(mesajlar);
        Assert.Equal(0, await _servis.OkunmamisToplamAsync(_amirId)); // okundu işaretlendi
    }

    [Fact]
    public async Task MesajlariGetir_YabanciTaraf_Yetkisiz()
    {
        await _servis.MesajGonderAsync(_amirId, _stajyerId, "Özel");

        // Başka amir bu konuşmayı çekmeye çalışırsa yetkisiz.
        var baskaAmirYetki = new YetkiBaglami(_baskaAmirId, false, true, false);
        var (yetkili, _) = await _servis.MesajlariGetirAsync(_baskaAmirId, _stajyerId, baskaAmirYetki, okunduIsaretle: false);

        Assert.False(yetkili);
    }

    [Fact]
    public async Task Admin_IzleyebilirAmaOkunduIsaretlemez()
    {
        await _servis.MesajGonderAsync(_stajyerId, _amirId, "Gizli");

        var adminYetki = new YetkiBaglami("admin-id", true, false, false);
        var (yetkili, mesajlar) = await _servis.MesajlariGetirAsync(_amirId, _stajyerId, adminYetki, okunduIsaretle: true);

        Assert.True(yetkili); // admin izler
        Assert.Single(mesajlar);
        Assert.Equal(1, await _servis.OkunmamisToplamAsync(_amirId)); // admin izleme okundu SAYMAZ
    }

    [Fact]
    public async Task Konusmalarim_Amir_StajyerleriniGetirir()
    {
        var amirYetki = new YetkiBaglami(_amirId, false, true, false);
        var liste = await _servis.KonusmalarimAsync(amirYetki);

        Assert.Single(liste);
        Assert.Equal(_stajyerId, liste[0].KullaniciId);
    }

    [Fact]
    public async Task Konusmalarim_Stajyer_AmiriniGetirir()
    {
        var stajyerYetki = new YetkiBaglami(_stajyerId, false, false, true);
        var liste = await _servis.KonusmalarimAsync(stajyerYetki);

        Assert.Single(liste);
        Assert.Equal(_amirId, liste[0].KullaniciId);
    }

    public void Dispose() => _veritabani.Dispose();
}
