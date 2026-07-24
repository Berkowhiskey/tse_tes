using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TES.Domain.Entities;
using TES.Infrastructure.Services;
using TES.Infrastructure.Simulation;

namespace TES.Tests;

/// <summary>
/// TSE-Misafir sponsor akışı testleri (CLAUDE.md Bölüm 8): sponsor doğrulama, rate-limit,
/// tek kullanımlık token, hash'li saklama, voucher/cihaz limiti ve otomatik iptal.
/// </summary>
public class MisafirTalepServisiTests : IDisposable
{
    private const string GecerliSponsor = "mehmet.demir@tse.org.tr";
    private const string LinkSablonu = "http://test/Misafir/Onay?token={0}";

    private readonly TestVeritabani _veritabani = new();
    private readonly MisafirTalepServisi _servis;
    private readonly SimulatedNetworkAccessProvider _agSaglayici;
    private readonly MisafirAyarlari _ayarlar = new()
    {
        TokenGecerlilikDakika = 15,
        SponsorSaatlikTalepLimiti = 3,
        CihazLimiti = 2
    };

    public MisafirTalepServisiTests()
    {
        _agSaglayici = new SimulatedNetworkAccessProvider(
            _veritabani.Db, NullLogger<SimulatedNetworkAccessProvider>.Instance);

        _servis = new MisafirTalepServisi(
            _veritabani.Db,
            new MockPersonelDizini(_veritabani.Db),
            new MockEmailSender(_veritabani.Db, NullLogger<MockEmailSender>.Instance),
            _agSaglayici,
            new DenetimServisi(_veritabani.Db),
            Options.Create(_ayarlar));
    }

    private Task<TalepSonucu> TalepOlusturAsync(string sponsor = GecerliSponsor) =>
        _servis.TalepOlusturAsync(
            new YeniMisafirTalebi("Misafir Kişi", "misafir@ornek.com", sponsor, 1, null), LinkSablonu);

    /// <summary>Simüle e-posta kutusundan son gönderilen onay token'ını söker.</summary>
    private string SonTokeniAl()
    {
        var eposta = _veritabani.Db.GidenEpostalar
            .OrderByDescending(e => e.Id)
            .First(e => e.Icerik.Contains("token="));

        var baslangic = eposta.Icerik.IndexOf("token=", StringComparison.Ordinal) + "token=".Length;
        var bitis = eposta.Icerik.IndexOfAny(['\n', '\r', ' '], baslangic);
        return eposta.Icerik[baslangic..(bitis < 0 ? eposta.Icerik.Length : bitis)];
    }

    [Fact]
    public async Task DizindeOlmayanSponsor_Reddedilir()
    {
        var sonuc = await TalepOlusturAsync("tanimsiz.kisi@tse.org.tr");

        Assert.False(sonuc.Basarili);
        Assert.Empty(_veritabani.Db.MisafirErisimTalepleri);
    }

    [Fact]
    public async Task TseDisiAdres_Reddedilir()
    {
        var sonuc = await TalepOlusturAsync("mehmet.demir@baskasirket.com");

        Assert.False(sonuc.Basarili);
    }

    [Fact]
    public async Task GecerliTalep_BeklemedeOlusur_TokenHashliSaklanir_SponsoraEpostaGider()
    {
        var sonuc = await TalepOlusturAsync();

        Assert.True(sonuc.Basarili);
        var talep = Assert.Single(_veritabani.Db.MisafirErisimTalepleri);
        Assert.Equal(MisafirTalepDurumu.Beklemede, talep.Durum);

        // Ham token e-postada; veritabanında yalnızca SHA-256 hash'i.
        var hamToken = SonTokeniAl();
        Assert.NotEqual(hamToken, talep.TokenHash);
        Assert.Equal(TokenYardimcisi.Hashle(hamToken), talep.TokenHash);

        var eposta = Assert.Single(_veritabani.Db.GidenEpostalar);
        Assert.Equal(GecerliSponsor, eposta.Kime);
    }

    [Fact]
    public async Task RateLimit_AsilincaTalepReddedilir()
    {
        for (var i = 0; i < _ayarlar.SponsorSaatlikTalepLimiti; i++)
            Assert.True((await TalepOlusturAsync()).Basarili);

        var asanTalep = await TalepOlusturAsync();

        Assert.False(asanTalep.Basarili);
        Assert.Equal(_ayarlar.SponsorSaatlikTalepLimiti, _veritabani.Db.MisafirErisimTalepleri.Count());
    }

    [Fact]
    public async Task Onay_EnabledYapar_AgErisiminiAcar_VoucherEpostalar()
    {
        await TalepOlusturAsync();
        var token = SonTokeniAl();

        var (basarili, _) = await _servis.KararVerAsync(token, onay: true);

        Assert.True(basarili);
        var talep = _veritabani.Db.MisafirErisimTalepleri.Single();
        Assert.Equal(MisafirTalepDurumu.Enabled, talep.Durum);
        Assert.NotNull(talep.VoucherHash);
        Assert.NotNull(talep.ErisimBitis);

        // Enforcement katmanı (simüle firewall) Enabled olmalı.
        var agDurumu = await _agSaglayici.DurumGetirAsync(talep.Id);
        Assert.Equal(SimuleAgDurumu.Enabled, agDurumu!.Durum);

        // Voucher talep sahibine e-postalanır; DB'de yalnızca hash durur.
        var voucherEpostasi = _veritabani.Db.GidenEpostalar.Single(e => e.Kime == "misafir@ornek.com");
        Assert.Contains("voucher", voucherEpostasi.Icerik, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Token_TekKullanimliktir()
    {
        await TalepOlusturAsync();
        var token = SonTokeniAl();

        Assert.True((await _servis.KararVerAsync(token, onay: true)).Basarili);
        var (ikinciKullanim, _) = await _servis.KararVerAsync(token, onay: true);

        Assert.False(ikinciKullanim);
    }

    [Fact]
    public async Task Red_DeniedYaparVeTalepSahibiniBilgilendirir()
    {
        await TalepOlusturAsync();
        var token = SonTokeniAl();

        var (basarili, _) = await _servis.KararVerAsync(token, onay: false);

        Assert.True(basarili);
        Assert.Equal(MisafirTalepDurumu.Denied, _veritabani.Db.MisafirErisimTalepleri.Single().Durum);
        Assert.Contains(_veritabani.Db.GidenEpostalar, e => e.Kime == "misafir@ornek.com");
    }

    [Fact]
    public async Task Voucher_CihazEkler_VeLimitiAsinca_Reddeder()
    {
        await TalepOlusturAsync();
        await _servis.KararVerAsync(SonTokeniAl(), onay: true);

        var voucherEpostasi = _veritabani.Db.GidenEpostalar.Single(e => e.Kime == "misafir@ornek.com");
        var satir = voucherEpostasi.Icerik.Split('\n').First(s => s.Contains("voucher kodunuz:"));
        var voucher = satir.Split(':')[^1].Trim();

        Assert.True((await _servis.VoucherKullanAsync(voucher)).Basarili);          // cihaz 1
        Assert.True((await _servis.VoucherKullanAsync(voucher)).Basarili);          // cihaz 2 (limit)
        Assert.False((await _servis.VoucherKullanAsync(voucher)).Basarili);         // limit doldu
        Assert.False((await _servis.VoucherKullanAsync("YANLIS-KOD-0000")).Basarili); // geçersiz voucher
    }

    [Fact]
    public async Task YanitsizTalep_OtomatikIptalEdilir()
    {
        await TalepOlusturAsync();
        var talep = _veritabani.Db.MisafirErisimTalepleri.Single();
        talep.TokenSonGecerlilik = DateTime.UtcNow.AddMinutes(-1); // süresi geçmiş gibi
        await _veritabani.Db.SaveChangesAsync();

        var islenen = await _servis.SuresiGecenleriIsleAsync();

        Assert.Equal(1, islenen);
        Assert.Equal(MisafirTalepDurumu.Expired, _veritabani.Db.MisafirErisimTalepleri.Single().Durum);
    }

    [Fact]
    public async Task SuresiBitenErisim_ExpiredOlurVeAgKapatilir()
    {
        await TalepOlusturAsync();
        await _servis.KararVerAsync(SonTokeniAl(), onay: true);

        var talep = _veritabani.Db.MisafirErisimTalepleri.Single();
        talep.ErisimBitis = DateTime.UtcNow.AddMinutes(-1); // süresi dolmuş gibi
        await _veritabani.Db.SaveChangesAsync();

        var islenen = await _servis.SuresiGecenleriIsleAsync();

        Assert.Equal(1, islenen);
        Assert.Equal(MisafirTalepDurumu.Expired, _veritabani.Db.MisafirErisimTalepleri.Single().Durum);

        var agDurumu = await _agSaglayici.DurumGetirAsync(talep.Id);
        Assert.Equal(SimuleAgDurumu.Expired, agDurumu!.Durum);
    }

    [Fact]
    public async Task SponsorKarar_DogruSponsorOnaylayabilir_EnabledYapar()
    {
        await TalepOlusturAsync(); // sponsor: mehmet.demir@tse.org.tr
        var talep = _veritabani.Db.MisafirErisimTalepleri.Single();

        var (basarili, _) = await _servis.SponsorKararVerAsync(talep.Id, GecerliSponsor, onay: true);

        Assert.True(basarili);
        Assert.Equal(MisafirTalepDurumu.Enabled, _veritabani.Db.MisafirErisimTalepleri.Single().Durum);
        Assert.Equal(GecerliSponsor, _veritabani.Db.MisafirErisimTalepleri.Single().KarariVeren);
    }

    [Fact]
    public async Task SponsorKarar_BaskaSponsorReddedilir_TalepBeklemedeKalir()
    {
        await TalepOlusturAsync(); // sponsor: mehmet.demir@tse.org.tr
        var talep = _veritabani.Db.MisafirErisimTalepleri.Single();

        // Farklı bir amir (kendisi sponsor gösterilmemiş) onaylamaya çalışırsa reddedilmeli.
        var (basarili, hata) = await _servis.SponsorKararVerAsync(talep.Id, "fatma.kaya@tse.org.tr", onay: true);

        Assert.False(basarili);
        Assert.NotNull(hata);
        Assert.Equal(MisafirTalepDurumu.Beklemede, _veritabani.Db.MisafirErisimTalepleri.Single().Durum);
    }

    [Fact]
    public async Task ManuelOnay_BekleyenTalebiEnabledYapar()
    {
        await TalepOlusturAsync();
        var talep = _veritabani.Db.MisafirErisimTalepleri.Single();

        var (basarili, _) = await _servis.ManuelOnaylaAsync(talep.Id, "admin");

        Assert.True(basarili);
        Assert.Equal(MisafirTalepDurumu.Enabled, _veritabani.Db.MisafirErisimTalepleri.Single().Durum);
        Assert.Equal("admin", _veritabani.Db.MisafirErisimTalepleri.Single().KarariVeren);
    }

    public void Dispose() => _veritabani.Dispose();
}
