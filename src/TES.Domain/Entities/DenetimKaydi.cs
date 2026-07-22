namespace TES.Domain.Entities;

/// <summary>
/// Hassas işlemlerin denetim izi (giriş, rol değişikliği, moderasyon, misafir onayı vb.).
/// Kişisel veri, parola veya token içeremez — yalnızca işlemin kendisi kaydedilir.
/// </summary>
public class DenetimKaydi
{
    public int Id { get; set; }

    /// <summary>İşlemi yapan aktör (kullanıcı adı veya "Sistem").</summary>
    public required string Aktor { get; set; }

    /// <summary>Yapılan işlemin kısa adı (örn. "Giris", "GirisBasarisiz", "ParolaDegisti").</summary>
    public required string Islem { get; set; }

    /// <summary>İşlem zamanı (UTC).</summary>
    public DateTime Zaman { get; set; } = DateTime.UtcNow;

    /// <summary>İsteğe bağlı ayrıntı — hassas veri İÇERMEZ.</summary>
    public string? Detay { get; set; }
}
