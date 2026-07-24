using System.ComponentModel.DataAnnotations;
using TES.Domain.Entities;
using TES.Infrastructure.Services;

namespace TES.Web.ViewModels;

public class FeedViewModel
{
    [Required(ErrorMessage = "Gönderi içeriği boş olamaz.")]
    [StringLength(4000, ErrorMessage = "Gönderi en fazla {1} karakter olabilir.")]
    [Display(Name = "Ne paylaşmak istersiniz?")]
    public string YeniIcerik { get; set; } = string.Empty;

    public IReadOnlyList<GonderiOzeti> Gonderiler { get; set; } = [];

    /// <summary>Stajyer isesse yeni gönderisinin onaya gideceği bilgisini göstermek için.</summary>
    public bool OnayaTabi { get; set; }
}

public class ReddetViewModel
{
    public int GonderiId { get; set; }
    public string YazarAdSoyad { get; set; } = string.Empty;
    public string Icerik { get; set; } = string.Empty;

    [Required(ErrorMessage = "Red gerekçesi zorunludur.")]
    [StringLength(1000)]
    [Display(Name = "Red gerekçesi")]
    public string RedMesaji { get; set; } = string.Empty;
}

public static class ModerasyonMetin
{
    public static (string Etiket, string Renk) Rozet(ModerasyonDurumu durum) => durum switch
    {
        ModerasyonDurumu.Beklemede => ("Onay bekliyor", "bg-yellow-lt"),
        ModerasyonDurumu.Onaylandi => ("Yayında", "bg-green-lt"),
        _ => ("Reddedildi", "bg-red-lt")
    };
}
