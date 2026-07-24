using TES.Infrastructure.Identity;
using TES.Infrastructure.Services;
using TES.Infrastructure.Simulation;

namespace TES.Tests;

public class MockPersonelDiziniTests : IDisposable
{
    private readonly TestVeritabani _veritabani = new();
    private readonly MockPersonelDizini _dizin;

    public MockPersonelDiziniTests()
    {
        _dizin = new MockPersonelDizini(_veritabani.Db);
    }

    [Theory]
    [InlineData("deneme_amiri", "deneme.amiri@tse.org.tr")]
    [InlineData("mehmet_demir", "mehmet.demir@tse.org.tr")]
    public void EpostaUret_KullaniciAdindanKurumEpostasiUretir(string kullaniciAdi, string beklenen)
    {
        Assert.Equal(beklenen, KullaniciYonetimServisi.EpostaUret(kullaniciAdi));
    }

    [Fact]
    public async Task SabitListedekiPersonel_Taninir()
    {
        Assert.True(await _dizin.KayitliMiAsync("fatma.kaya@tse.org.tr"));
    }

    [Fact]
    public async Task TseDisiAdres_Taninmaz()
    {
        Assert.False(await _dizin.KayitliMiAsync("biri@gmail.com"));
    }

    [Fact]
    public async Task SistemdekiAmirEpostasi_DizindeTaninir()
    {
        // Sabit listede olmayan ama sisteme kayıtlı bir amir e-postası dizinde sayılmalı.
        _veritabani.Db.Users.Add(new Kullanici
        {
            UserName = "deneme_amiri",
            AdSoyad = "Deneme Amiri",
            Email = "deneme.amiri@tse.org.tr"
        });
        await _veritabani.Db.SaveChangesAsync();

        Assert.True(await _dizin.KayitliMiAsync("deneme.amiri@tse.org.tr"));
    }

    [Fact]
    public async Task SistemdeOlmayanTseAdresi_Taninmaz()
    {
        Assert.False(await _dizin.KayitliMiAsync("hayalet.amir@tse.org.tr"));
    }

    public void Dispose() => _veritabani.Dispose();
}
