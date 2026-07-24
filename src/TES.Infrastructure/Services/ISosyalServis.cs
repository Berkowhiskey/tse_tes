using TES.Domain.Entities;

namespace TES.Infrastructure.Services;

public record YorumOzeti(Yorum Yorum, string YazarAdSoyad, bool Silebilir);

public record GonderiOzeti(
    Gonderi Gonderi,
    string YazarAdSoyad,
    int BegeniSayisi,
    bool KullaniciBegendi,
    bool Silebilir,
    IReadOnlyList<YorumOzeti> Yorumlar);

public interface ISosyalServis
{
    /// <summary>
    /// Akış: onaylanmış tüm gönderiler + isteğin sahibinin kendi (bekleyen/reddedilen dahil)
    /// gönderileri. Yeniden eskiye sıralı. Silinebilirlik sahiplik/rol'e göre işaretlenir.
    /// </summary>
    Task<IReadOnlyList<GonderiOzeti>> FeedGetirAsync(YetkiBaglami yetki);

    /// <summary>
    /// Gönderi oluşturur. Stajyerinki Beklemede (admin onayına tabi); Amir/Admininki
    /// doğrudan Onaylandi (CLAUDE.md Bölüm 5). Oluşan gönderinin moderasyon durumunu döner.
    /// </summary>
    Task<ModerasyonDurumu> GonderiOlusturAsync(string icerik, YetkiBaglami yetki);

    /// <summary>Silme: yalnızca gönderinin sahibi veya Admin (Admin herkesinkini).</summary>
    Task<IsSonucu> GonderiSilAsync(int gonderiId, YetkiBaglami yetki);

    // ---- Moderasyon (yalnızca Admin) ----

    Task<IReadOnlyList<GonderiOzeti>> ModerasyonBekleyenlerAsync();

    Task<IsSonucu> OnaylaAsync(int gonderiId, string adminAdi);

    Task<IsSonucu> ReddetAsync(int gonderiId, string redMesaji, string adminAdi);

    // ---- Yorum & Beğeni (onaylanmış gönderilere) ----

    Task<IsSonucu> YorumEkleAsync(int gonderiId, string icerik, YetkiBaglami yetki);

    /// <summary>Yorum silme: yalnızca yorumun sahibi veya Admin.</summary>
    Task<IsSonucu> YorumSilAsync(int yorumId, YetkiBaglami yetki);

    /// <summary>Beğeniyi açar/kapatır; yeni durumu (beğenildi mi) döner.</summary>
    Task<(bool Basarili, bool Begenildi, string? Hata)> BegeniToggleAsync(int gonderiId, YetkiBaglami yetki);
}
