using Microsoft.AspNetCore.Identity;

namespace TES.Infrastructure.Identity;

/// <summary>
/// TES kullanıcısı. Parola yönetimi tamamen Identity'nin hash mekanizmasına aittir;
/// bu sınıfta asla düz metin parola/T.C. tutulmaz.
/// </summary>
public class Kullanici : IdentityUser
{
    /// <summary>Görünen ad soyad (örn. "Ayşe Yılmaz").</summary>
    public required string AdSoyad { get; set; }

    /// <summary>
    /// İlk (geçici) parolayla girişte true'dur; kullanıcı parolasını değiştirene kadar
    /// sistemin geri kalanına erişemez (CLAUDE.md Bölüm 7).
    /// </summary>
    public bool ForceChangePassword { get; set; }

    /// <summary>Hesap oluşturulma zamanı (UTC).</summary>
    public DateTime OlusturmaZamani { get; set; } = DateTime.UtcNow;

    /// <summary>Staj bitiminde pasifleştirme için (KVKK — ileride derinleştirilecek).</summary>
    public bool AktifMi { get; set; } = true;
}
