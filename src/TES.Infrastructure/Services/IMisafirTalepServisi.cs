using TES.Domain.Entities;

namespace TES.Infrastructure.Services;

/// <summary>Yeni talep bilgisi; stajyer talebiyse StajyerProfilId dolu gelir.</summary>
public record YeniMisafirTalebi(
    string AdSoyad,
    string Eposta,
    string SponsorEposta,
    int SureGun,
    int? StajyerProfilId);

public record TalepSonucu(bool Basarili, string? Hata, Guid? TakipKodu)
{
    public static TalepSonucu Basari(Guid takipKodu) => new(true, null, takipKodu);
    public static TalepSonucu Basarisiz(string hata) => new(false, hata, null);
}

public interface IMisafirTalepServisi
{
    /// <summary>
    /// Talep oluşturur: sponsor dizinde doğrulanır, rate-limit uygulanır, tek kullanımlık
    /// süreli token üretilir (yalnızca hash'i saklanır) ve sponsora onay e-postası gönderilir.
    /// onayLinkSablonu "{0}" yerine ham token konarak e-postaya yazılır.
    /// </summary>
    Task<TalepSonucu> TalepOlusturAsync(YeniMisafirTalebi bilgi, string onayLinkSablonu);

    /// <summary>Geçerli (Beklemede + süresi dolmamış) token'a ait talebi getirir; yoksa null.</summary>
    Task<MisafirErisimTalebi?> TokenIleGetirAsync(string token);

    /// <summary>
    /// Sponsor kararı. Token tek kullanımlıktır: karar sonrası aynı token geçersizdir.
    /// Onayda erişim INetworkAccessProvider ile açılır ve voucher talep sahibine e-postalanır.
    /// </summary>
    Task<(bool Basarili, string? Hata)> KararVerAsync(string token, bool onay);

    /// <summary>Admin manuel bağlama: bekleyen talebi sponsorsuz onaylar (denetim kaydıyla).</summary>
    Task<(bool Basarili, string? Hata)> ManuelOnaylaAsync(int talepId, string adminAktor);

    /// <summary>Bir sponsora (e-posta) ait tüm talepler — amirin "Misafir Onayları" ekranı için.</summary>
    Task<IReadOnlyList<MisafirErisimTalebi>> SponsorunTalepleriAsync(string sponsorEposta);

    /// <summary>
    /// Sponsor kararı sistem içinden (giriş yapmış amir). Sahiplik SUNUCUDA doğrulanır:
    /// talep yalnızca kendisinin sponsor gösterildiği amir tarafından karara bağlanabilir.
    /// </summary>
    Task<(bool Basarili, string? Hata)> SponsorKararVerAsync(int talepId, string sponsorEposta, bool onay);

    /// <summary>Admin iptali: talebi Denied yapar, ağ erişimini kapatır.</summary>
    Task<(bool Basarili, string? Hata)> IptalAsync(int talepId, string aktor);

    /// <summary>Voucher ile yeni cihaz ekler (cihaz limiti denetlenir).</summary>
    Task<(bool Basarili, string Mesaj)> VoucherKullanAsync(string voucher);

    /// <summary>
    /// Otomatik iptal/süre dolumu taraması: yanıtlanmayan talepler Expired olur,
    /// süresi biten Enabled erişimler kapatılır. İşlenen kayıt sayısını döner.
    /// </summary>
    Task<int> SuresiGecenleriIsleAsync();

    Task<MisafirErisimTalebi?> TakipKoduIleGetirAsync(Guid takipKodu);

    Task<IReadOnlyList<MisafirErisimTalebi>> StajyerinTalepleriAsync(string kullaniciId);

    Task<IReadOnlyList<MisafirErisimTalebi>> TumTalepleriAsync();
}
