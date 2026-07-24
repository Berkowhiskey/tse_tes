using System.ComponentModel.DataAnnotations;
using TES.Domain.Entities;
using TES.Infrastructure.Simulation;

namespace TES.Web.ViewModels;

public class MisafirTalepFormViewModel
{
    [Required(ErrorMessage = "Ad Soyad zorunludur.")]
    [StringLength(128)]
    [Display(Name = "Ad Soyad")]
    public string AdSoyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [StringLength(256)]
    [Display(Name = "E-posta adresiniz")]
    public string Eposta { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sponsor e-postası zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [StringLength(256)]
    [Display(Name = "Sponsor e-postası (@tse.org.tr)")]
    public string SponsorEposta { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Süre 1, 3 veya 5 gün olmalıdır.")]
    [Display(Name = "Erişim süresi")]
    public int SureGun { get; set; } = 1;
}

/// <summary>
/// Stajyerin talep formu: Ad Soyad alanı YOKTUR — sunucuda oturumdaki kimlikten alınır
/// (formdan gelene güvenilmez). Anonim portaldaki form MisafirTalepFormViewModel kullanır.
/// </summary>
public class StajyerTalepFormViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [StringLength(256)]
    [Display(Name = "E-posta adresiniz")]
    public string Eposta { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sponsor e-postası zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [StringLength(256)]
    [Display(Name = "Sponsor e-postası (@tse.org.tr)")]
    public string SponsorEposta { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Süre 1, 3 veya 5 gün olmalıdır.")]
    [Display(Name = "Erişim süresi")]
    public int SureGun { get; set; } = 1;
}

public class MisafirDurumViewModel
{
    public required MisafirErisimTalebi Talep { get; init; }
    public SimuleAgErisimi? AgDurumu { get; init; }

    public (string Etiket, string Renk) DurumRozeti => Talep.Durum switch
    {
        MisafirTalepDurumu.Beklemede => ("Sponsor onayı bekleniyor", "bg-yellow-lt"),
        MisafirTalepDurumu.Enabled => ("Erişim aktif (Enabled)", "bg-green-lt"),
        MisafirTalepDurumu.Expired => ("Süresi doldu (Expired)", "bg-secondary-lt"),
        _ => ("Reddedildi (Denied)", "bg-red-lt")
    };
}

public class MisafirOnayViewModel
{
    public required string Token { get; init; }
    public required MisafirErisimTalebi Talep { get; init; }
}

public class VoucherViewModel
{
    [Required(ErrorMessage = "Voucher kodu zorunludur.")]
    [Display(Name = "Voucher kodu")]
    public string Voucher { get; set; } = string.Empty;

    public string? Mesaj { get; set; }
    public bool? Basarili { get; set; }
}

public class TaleplerimViewModel
{
    public StajyerTalepFormViewModel Form { get; set; } = new();
    public IReadOnlyList<MisafirErisimTalebi> Talepler { get; set; } = [];
}

public class MisafirYonetimSatiri
{
    public required MisafirErisimTalebi Talep { get; init; }
    public SimuleAgErisimi? AgDurumu { get; init; }
}
