using System.ComponentModel.DataAnnotations;
using TES.Infrastructure.Services;

namespace TES.Web.ViewModels;

public class ProfilViewModel
{
    public string AdSoyad { get; set; } = string.Empty;
    public string KullaniciAdi { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;

    /// <summary>Stajyer için profil satırı (amir/admin'de null).</summary>
    public StajyerSatiri? StajyerBilgisi { get; set; }

    /// <summary>Amir için profil alanları.</summary>
    public string? DepartmanAd { get; set; }
    public DateOnly? IseBaslamaTarihi { get; set; }
    public string? Hakkimda { get; set; }
}

public class ProfilDuzenleViewModel
{
    // Stajyer kendi eğitim bilgilerini, amir "Hakkında"sını düzenleyebilir (CLAUDE.md Bölüm 5).
    [StringLength(256)]
    [Display(Name = "Okul")]
    public string? Okul { get; set; }

    [StringLength(256)]
    [Display(Name = "Bölüm")]
    public string? Bolum { get; set; }

    [StringLength(2000)]
    [Display(Name = "Hakkında")]
    public string? Hakkimda { get; set; }

    public bool StajyerMi { get; set; }
}
