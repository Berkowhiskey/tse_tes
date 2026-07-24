using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TES.Domain.Entities;
using TES.Infrastructure.Data;
using TES.Infrastructure.Simulation;

namespace TES.Infrastructure.Services;

/// <summary>
/// TSE-Misafir sponsor akışının karar katmanı (CLAUDE.md Bölüm 8).
/// Enforcement (paketlere izin) daima INetworkAccessProvider'dadır; burada yalnızca
/// form → onay → durum → voucher iş akışı yürür. Token/voucher ham halde saklanmaz.
/// </summary>
public class MisafirTalepServisi(
    TesDbContext db,
    IPersonelDizini personelDizini,
    IEmailSender emailSender,
    INetworkAccessProvider agSaglayici,
    IDenetimServisi denetim,
    IOptions<MisafirAyarlari> ayarlarOption) : IMisafirTalepServisi
{
    private readonly MisafirAyarlari _ayarlar = ayarlarOption.Value;

    public async Task<TalepSonucu> TalepOlusturAsync(YeniMisafirTalebi bilgi, string onayLinkSablonu)
    {
        if (!_ayarlar.GecerliSureler.Contains(bilgi.SureGun))
            return TalepSonucu.Basarisiz("Geçersiz süre. İzin verilen süreler: 1, 3 veya 5 gün.");

        // Sponsor serbest yazılır ama gönderim ÖNCESİ dizinde doğrulanır.
        if (!await personelDizini.KayitliMiAsync(bilgi.SponsorEposta))
            return TalepSonucu.Basarisiz("Sponsor e-postası kayıtlı bir @tse.org.tr personeline ait değil.");

        // Rate-limit: bir sponsora istek yağmuru engellenir.
        var birSaatOnce = DateTime.UtcNow.AddHours(-1);
        var sonTalepSayisi = await db.MisafirErisimTalepleri
            .CountAsync(t => t.SponsorEposta == bilgi.SponsorEposta && t.OlusturmaZamani >= birSaatOnce);

        if (sonTalepSayisi >= _ayarlar.SponsorSaatlikTalepLimiti)
            return TalepSonucu.Basarisiz("Bu sponsora kısa süre içinde çok fazla talep gönderildi. Lütfen daha sonra deneyin.");

        // Tek kullanımlık, süreli, kriptografik token — yalnızca hash'i saklanır.
        var token = TokenYardimcisi.TokenUret();

        var talep = new MisafirErisimTalebi
        {
            StajyerProfilId = bilgi.StajyerProfilId,
            AdSoyad = bilgi.AdSoyad.Trim(),
            Eposta = bilgi.Eposta.Trim(),
            SponsorEposta = bilgi.SponsorEposta.Trim(),
            SureGun = bilgi.SureGun,
            TokenHash = TokenYardimcisi.Hashle(token),
            TokenSonGecerlilik = DateTime.UtcNow.AddMinutes(_ayarlar.TokenGecerlilikDakika)
        };

        db.MisafirErisimTalepleri.Add(talep);
        await db.SaveChangesAsync();

        var onayLinki = string.Format(onayLinkSablonu, token);
        await emailSender.GonderAsync(
            talep.SponsorEposta,
            "TSE-Misafir erişim onayı bekleniyor",
            $"Sayın yetkili,\n\n{talep.AdSoyad} ({talep.Eposta}) {talep.SureGun} günlük TSE-Misafir ağı erişimi için " +
            $"sizi sponsor gösterdi.\n\nOnaylamak veya reddetmek için (bağlantı {_ayarlar.TokenGecerlilikDakika} dakika " +
            $"geçerlidir, tek kullanımlıktır):\n{onayLinki}\n\nSüresinde yanıtlanmayan talepler otomatik iptal edilir.");

        await denetim.KaydetAsync(talep.AdSoyad, "MisafirTalebiOlusturuldu",
            $"Talep: {talep.Id}, sponsor: {talep.SponsorEposta}, süre: {talep.SureGun} gün");

        return TalepSonucu.Basari(talep.TakipKodu);
    }

    public async Task<MisafirErisimTalebi?> TokenIleGetirAsync(string token)
    {
        var hash = TokenYardimcisi.Hashle(token);

        return await db.MisafirErisimTalepleri
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == hash
                                      && t.Durum == MisafirTalepDurumu.Beklemede
                                      && t.TokenSonGecerlilik >= DateTime.UtcNow);
    }

    public async Task<(bool Basarili, string? Hata)> KararVerAsync(string token, bool onay)
    {
        var hash = TokenYardimcisi.Hashle(token);

        // Tek kullanımlık: yalnızca Beklemede'deki talep karara bağlanabilir; karar sonrası
        // durum değiştiğinden aynı token bir daha eşleşmez.
        var talep = await db.MisafirErisimTalepleri
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.Durum == MisafirTalepDurumu.Beklemede);

        if (talep is null)
            return (false, "Bağlantı geçersiz veya daha önce kullanılmış.");

        if (talep.TokenSonGecerlilik < DateTime.UtcNow)
            return (false, "Bağlantının süresi dolmuş. Talep otomatik iptal edilecektir.");

        return onay
            ? await OnaylaAsync(talep, talep.SponsorEposta)
            : await ReddetAsync(talep, talep.SponsorEposta);
    }

    public async Task<(bool Basarili, string? Hata)> ManuelOnaylaAsync(int talepId, string adminAktor)
    {
        var talep = await db.MisafirErisimTalepleri
            .FirstOrDefaultAsync(t => t.Id == talepId && t.Durum == MisafirTalepDurumu.Beklemede);

        if (talep is null)
            return (false, "Bekleyen talep bulunamadı.");

        return await OnaylaAsync(talep, adminAktor, manuel: true);
    }

    public async Task<IReadOnlyList<MisafirErisimTalebi>> SponsorunTalepleriAsync(string sponsorEposta)
    {
        return await db.MisafirErisimTalepleri
            .AsNoTracking()
            .Where(t => t.SponsorEposta == sponsorEposta)
            .OrderByDescending(t => t.OlusturmaZamani)
            .ToListAsync();
    }

    public async Task<(bool Basarili, string? Hata)> SponsorKararVerAsync(int talepId, string sponsorEposta, bool onay)
    {
        var talep = await db.MisafirErisimTalepleri
            .FirstOrDefaultAsync(t => t.Id == talepId && t.Durum == MisafirTalepDurumu.Beklemede);

        if (talep is null)
            return (false, "Bekleyen talep bulunamadı.");

        // Sahiplik kuralı sunucuda: yalnızca kendisinin sponsor gösterildiği talep karara bağlanabilir.
        if (!string.Equals(talep.SponsorEposta, sponsorEposta, StringComparison.OrdinalIgnoreCase))
            return (false, "Bu talebin sponsoru siz değilsiniz.");

        return onay
            ? await OnaylaAsync(talep, sponsorEposta)
            : await ReddetAsync(talep, sponsorEposta);
    }

    public async Task<(bool Basarili, string? Hata)> IptalAsync(int talepId, string aktor)
    {
        var talep = await db.MisafirErisimTalepleri.FirstOrDefaultAsync(t => t.Id == talepId);
        if (talep is null)
            return (false, "Talep bulunamadı.");

        if (talep.Durum is MisafirTalepDurumu.Expired or MisafirTalepDurumu.Denied)
            return (false, "Talep zaten kapalı.");

        talep.Durum = MisafirTalepDurumu.Denied;
        talep.KararZamani = DateTime.UtcNow;
        talep.KarariVeren = aktor;
        await db.SaveChangesAsync();

        await agSaglayici.ErisimKapatAsync(talep.Id);
        await denetim.KaydetAsync(aktor, "MisafirTalebiIptalEdildi", $"Talep: {talep.Id}");

        return (true, null);
    }

    public async Task<(bool Basarili, string Mesaj)> VoucherKullanAsync(string voucher)
    {
        var hash = TokenYardimcisi.Hashle(voucher.Trim().ToUpperInvariant());

        var talep = await db.MisafirErisimTalepleri
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.VoucherHash == hash && t.Durum == MisafirTalepDurumu.Enabled);

        if (talep is null)
            return (false, "Voucher geçersiz veya erişim aktif değil.");

        if (talep.ErisimBitis < DateTime.UtcNow)
            return (false, "Voucher'ın süresi dolmuş.");

        var (basarili, cihazSayisi) = await agSaglayici.CihazKaydetAsync(talep.Id, _ayarlar.CihazLimiti);

        if (!basarili)
            return (false, cihazSayisi >= _ayarlar.CihazLimiti
                ? $"Cihaz limiti doldu ({_ayarlar.CihazLimiti} cihaz)."
                : "Cihaz eklenemedi; erişim aktif değil.");

        await denetim.KaydetAsync(talep.AdSoyad, "MisafirCihazEklendi",
            $"Talep: {talep.Id}, cihaz: {cihazSayisi}/{_ayarlar.CihazLimiti}");

        return (true, $"Cihaz ağa eklendi ({cihazSayisi}/{_ayarlar.CihazLimiti}). " +
                      $"Erişim bitişi: {talep.ErisimBitis:dd.MM.yyyy HH:mm} (UTC)");
    }

    public async Task<int> SuresiGecenleriIsleAsync()
    {
        var simdi = DateTime.UtcNow;
        var islenen = 0;

        // 1) Sponsor süresinde yanıtlamadı → otomatik iptal (Expired).
        var yanitsizlar = await db.MisafirErisimTalepleri
            .Where(t => t.Durum == MisafirTalepDurumu.Beklemede && t.TokenSonGecerlilik < simdi)
            .ToListAsync();

        foreach (var talep in yanitsizlar)
        {
            talep.Durum = MisafirTalepDurumu.Expired;
            talep.KararZamani = simdi;
            talep.KarariVeren = "Sistem (zaman aşımı)";
            islenen++;

            await denetim.KaydetAsync("Sistem", "MisafirTalebiOtomatikIptal",
                $"Talep: {talep.Id} — sponsor süresinde yanıtlamadı.");
        }

        // 2) Erişim süresi biten Enabled talepler → Expired + ağ erişimi kapatılır.
        var bitenler = await db.MisafirErisimTalepleri
            .Where(t => t.Durum == MisafirTalepDurumu.Enabled && t.ErisimBitis < simdi)
            .ToListAsync();

        foreach (var talep in bitenler)
        {
            talep.Durum = MisafirTalepDurumu.Expired;
            islenen++;

            await agSaglayici.ErisimKapatAsync(talep.Id);
            await denetim.KaydetAsync("Sistem", "MisafirErisimiSuresiDoldu", $"Talep: {talep.Id}");
        }

        if (islenen > 0)
            await db.SaveChangesAsync();

        return islenen;
    }

    public Task<MisafirErisimTalebi?> TakipKoduIleGetirAsync(Guid takipKodu) =>
        db.MisafirErisimTalepleri.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TakipKodu == takipKodu);

    public async Task<IReadOnlyList<MisafirErisimTalebi>> StajyerinTalepleriAsync(string kullaniciId)
    {
        return await db.MisafirErisimTalepleri
            .AsNoTracking()
            .Where(t => t.StajyerProfil != null && t.StajyerProfil.KullaniciId == kullaniciId)
            .OrderByDescending(t => t.OlusturmaZamani)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MisafirErisimTalebi>> TumTalepleriAsync()
    {
        return await db.MisafirErisimTalepleri
            .AsNoTracking()
            .OrderByDescending(t => t.OlusturmaZamani)
            .ToListAsync();
    }

    private async Task<(bool, string?)> OnaylaAsync(MisafirErisimTalebi talep, string karariVeren, bool manuel = false)
    {
        // Voucher üretilir; yalnızca hash'i saklanır, ham hali talep sahibine e-postalanır.
        var voucher = TokenYardimcisi.VoucherUret();

        talep.Durum = MisafirTalepDurumu.Enabled;
        talep.VoucherHash = TokenYardimcisi.Hashle(voucher);
        talep.ErisimBitis = DateTime.UtcNow.AddDays(talep.SureGun);
        talep.KararZamani = DateTime.UtcNow;
        talep.KarariVeren = karariVeren;
        await db.SaveChangesAsync();

        // Enforcement daima arayüz üzerinden (CLAUDE.md Bölüm 13).
        await agSaglayici.ErisimVerAsync(talep.Id, talep.ErisimBitis.Value);

        await emailSender.GonderAsync(
            talep.Eposta,
            "TSE-Misafir erişiminiz onaylandı",
            $"Sayın {talep.AdSoyad},\n\nTSE-Misafir ağı erişiminiz {talep.SureGun} gün süreyle onaylandı.\n" +
            $"Erişim bitişi: {talep.ErisimBitis:dd.MM.yyyy HH:mm} (UTC)\n\n" +
            $"Diğer cihazlarınızı bağlamak için voucher kodunuz: {voucher}\n" +
            $"(En fazla {_ayarlar.CihazLimiti} cihaz)");

        await denetim.KaydetAsync(karariVeren,
            manuel ? "MisafirTalebiManuelOnaylandi" : "MisafirTalebiOnaylandi",
            $"Talep: {talep.Id}, süre: {talep.SureGun} gün");

        return (true, null);
    }

    private async Task<(bool, string?)> ReddetAsync(MisafirErisimTalebi talep, string karariVeren)
    {
        talep.Durum = MisafirTalepDurumu.Denied;
        talep.KararZamani = DateTime.UtcNow;
        talep.KarariVeren = karariVeren;
        await db.SaveChangesAsync();

        await emailSender.GonderAsync(
            talep.Eposta,
            "TSE-Misafir erişim talebiniz reddedildi",
            $"Sayın {talep.AdSoyad},\n\nTSE-Misafir ağı erişim talebiniz sponsor tarafından reddedildi.");

        await denetim.KaydetAsync(karariVeren, "MisafirTalebiReddedildi", $"Talep: {talep.Id}");

        return (true, null);
    }
}
