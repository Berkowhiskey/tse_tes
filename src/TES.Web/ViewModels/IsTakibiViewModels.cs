using System.ComponentModel.DataAnnotations;
using TES.Domain.Entities;

namespace TES.Web.ViewModels;

// ---- Proje ----

public class ProjeYonetViewModel
{
    public int StajyerProfilId { get; set; }
    public string StajyerAdSoyad { get; set; } = string.Empty;
    public bool Duzenleme { get; set; }

    [Required(ErrorMessage = "Proje adı zorunludur.")]
    [StringLength(200)]
    [Display(Name = "Proje Adı")]
    public string Ad { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }
}

public class ProjemViewModel
{
    public Proje? Proje { get; set; }
}

// ---- Ödev ----

public class OdevAtaViewModel
{
    public int StajyerProfilId { get; set; }
    public string StajyerAdSoyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(200)]
    [Display(Name = "Başlık")]
    public string Baslik { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Teslim Tarihi")]
    public DateOnly? TeslimTarihi { get; set; }
}

/// <summary>Ödev listelerinde stajyer adını da göstermek için sarmalayıcı.</summary>
public record OdevSatiri(Odev Odev, string? StajyerAdSoyad);

public static class IsDurumuMetin
{
    public static string Etiket(IsDurumu durum) => durum switch
    {
        IsDurumu.Baslamadi => "Başlamadı",
        IsDurumu.DevamEdiyor => "Devam Ediyor",
        IsDurumu.Tamamlandi => "Tamamlandı",
        _ => durum.ToString()
    };

    public static string Renk(IsDurumu durum) => durum switch
    {
        IsDurumu.Baslamadi => "bg-secondary-lt",
        IsDurumu.DevamEdiyor => "bg-blue-lt",
        IsDurumu.Tamamlandi => "bg-green-lt",
        _ => "bg-secondary-lt"
    };
}
