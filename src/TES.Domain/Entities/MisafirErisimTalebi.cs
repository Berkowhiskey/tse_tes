namespace TES.Domain.Entities;

/// <summary>
/// TSE-Misafir talep durumları (CLAUDE.md Bölüm 6 — İngilizce durum adları künyeden gelir).
/// </summary>
public enum MisafirTalepDurumu
{
    Beklemede = 0,
    Enabled = 1,
    Expired = 2,
    Denied = 3
}

/// <summary>
/// TSE-Misafir sponsorlu erişim talebi (CLAUDE.md Bölüm 8).
/// GÜVENLİK: Onay token'ı ve voucher düz metin SAKLANMAZ — yalnızca SHA-256 hash'leri tutulur.
/// Ham değerler yalnızca simüle e-posta (teslimat kanalı) üzerinden kullanıcıya ulaşır.
/// </summary>
public class MisafirErisimTalebi
{
    public int Id { get; set; }

    /// <summary>Durum sayfası için tahmin edilemez takip kodu (URL'de Id yerine kullanılır).</summary>
    public Guid TakipKodu { get; set; } = Guid.NewGuid();

    /// <summary>TES'e kayıtlı stajyer talebiyse dolu; anonim misafirde null.</summary>
    public int? StajyerProfilId { get; set; }
    public StajyerProfil? StajyerProfil { get; set; }

    public required string AdSoyad { get; set; }

    /// <summary>Talep sahibinin e-postası (voucher buraya gönderilir).</summary>
    public required string Eposta { get; set; }

    /// <summary>Serbest yazılır; gönderim ÖNCESİ personel dizininde doğrulanır.</summary>
    public required string SponsorEposta { get; set; }

    /// <summary>Erişim süresi: 1 / 3 / 5 gün.</summary>
    public int SureGun { get; set; }

    public MisafirTalepDurumu Durum { get; set; } = MisafirTalepDurumu.Beklemede;

    /// <summary>Onay token'ının SHA-256 hash'i — tek kullanımlık, süreli.</summary>
    public required string TokenHash { get; set; }

    /// <summary>Token bu andan sonra geçersizdir; sponsor yanıtlamazsa talep otomatik iptal edilir.</summary>
    public DateTime TokenSonGecerlilik { get; set; }

    /// <summary>Onayda üretilen voucher'ın SHA-256 hash'i (ham hali talep sahibine e-postayla gider).</summary>
    public string? VoucherHash { get; set; }

    /// <summary>Enabled durumunda erişimin biteceği an (SureGun'den hesaplanır).</summary>
    public DateTime? ErisimBitis { get; set; }

    public DateTime OlusturmaZamani { get; set; } = DateTime.UtcNow;

    public DateTime? KararZamani { get; set; }

    /// <summary>Kararı veren: sponsor e-postası veya admin kullanıcı adı (manuel bağlama).</summary>
    public string? KarariVeren { get; set; }
}
