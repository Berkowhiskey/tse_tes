using TES.Infrastructure.Services;

namespace TES.Tests;

public class DepartmanServisiTests : IDisposable
{
    private readonly TestVeritabani _veritabani = new();
    private readonly DepartmanServisi _servis;

    public DepartmanServisiTests()
    {
        _servis = new DepartmanServisi(_veritabani.Db, new DenetimServisi(_veritabani.Db));
    }

    [Fact]
    public async Task HiyerarsikListe_SeviyeleriDogruHesaplar()
    {
        var kok = await _servis.OlusturAsync("Bilgi-İşlem", null, "test");
        await _servis.OlusturAsync("Yazılım Geliştirme", kok.Id, "test");

        var liste = await _servis.HiyerarsikListeAsync();

        Assert.Equal(2, liste.Count);
        Assert.Equal(0, liste[0].Seviye);
        Assert.Equal("Bilgi-İşlem", liste[0].Departman.Ad);
        Assert.Equal(1, liste[1].Seviye);
        Assert.Equal("Yazılım Geliştirme", liste[1].Departman.Ad);
    }

    [Fact]
    public async Task AltDepartmaniOlan_Silinemez()
    {
        var kok = await _servis.OlusturAsync("Bilgi-İşlem", null, "test");
        await _servis.OlusturAsync("Sistem", kok.Id, "test");

        var (basarili, hata) = await _servis.SilAsync(kok.Id, "test");

        Assert.False(basarili);
        Assert.NotNull(hata);
    }

    [Fact]
    public async Task Guncelle_KendiAltAgacinaTasinamaz()
    {
        var kok = await _servis.OlusturAsync("Bilgi-İşlem", null, "test");
        var alt = await _servis.OlusturAsync("Sistem", kok.Id, "test");

        // Kök departmanı kendi altındaki "Sistem"in altına taşımak döngü oluşturur.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _servis.GuncelleAsync(kok.Id, "Bilgi-İşlem", alt.Id, "test"));
    }

    [Fact]
    public async Task BosDepartman_Silinebilir()
    {
        var departman = await _servis.OlusturAsync("Geçici Birim", null, "test");

        var (basarili, _) = await _servis.SilAsync(departman.Id, "test");

        Assert.True(basarili);
        Assert.Empty(_veritabani.Db.Departmanlar);
    }

    public void Dispose() => _veritabani.Dispose();
}
