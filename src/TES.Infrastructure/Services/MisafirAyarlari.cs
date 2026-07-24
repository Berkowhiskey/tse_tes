namespace TES.Infrastructure.Services;

/// <summary>
/// TSE-Misafir akışı ayarları (appsettings "MisafirAyarlari" bölümünden bağlanır).
/// Token süresi MVP testinde kısadır; prod'da 1-2 saate çıkarılır (CLAUDE.md Bölüm 8).
/// </summary>
public class MisafirAyarlari
{
    public const string Bolum = "MisafirAyarlari";

    /// <summary>Sponsor onay token'ının geçerlilik süresi (dakika).</summary>
    public int TokenGecerlilikDakika { get; set; } = 15;

    /// <summary>Bir sponsora saatte en fazla kaç talep gönderilebilir (rate-limit).</summary>
    public int SponsorSaatlikTalepLimiti { get; set; } = 3;

    /// <summary>Bir voucher ile en fazla kaç cihaz eklenebilir.</summary>
    public int CihazLimiti { get; set; } = 3;

    /// <summary>İzin verilen erişim süreleri (gün).</summary>
    public int[] GecerliSureler { get; set; } = [1, 3, 5];
}
