using System.ComponentModel.DataAnnotations;

namespace TES.Web.ViewModels;

public class ParolaDegistirViewModel
{
    [Required(ErrorMessage = "Mevcut parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Parola")]
    public string MevcutParola { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni parola zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Parola en az {2} karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Parola")]
    public string YeniParola { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola tekrarı zorunludur.")]
    [Compare(nameof(YeniParola), ErrorMessage = "Parolalar eşleşmiyor.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Parola (Tekrar)")]
    public string YeniParolaTekrar { get; set; } = string.Empty;
}
