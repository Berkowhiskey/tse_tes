using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TES.Infrastructure.Services;

namespace TES.Web.ViewModels;

public class KullaniciYonetimIndexViewModel
{
    public IReadOnlyList<AmirSatiri> Amirler { get; set; } = [];
    public IReadOnlyList<StajyerSatiri> Stajyerler { get; set; } = [];
}

public class AmirOlusturViewModel
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(64)]
    [Display(Name = "Ad")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(64)]
    [Display(Name = "Soyad")]
    public string Soyad { get; set; } = string.Empty;

    // Yalnızca geçici parola üretiminde kullanılır; SAKLANMAZ ve loglanmaz.
    [Required(ErrorMessage = "T.C. Kimlik No zorunludur.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "T.C. Kimlik No 11 haneli rakam olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "T.C. Kimlik No (geçici parola olur)")]
    public string TcKimlikNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Departman seçimi zorunludur.")]
    [Display(Name = "Departman")]
    public int? DepartmanId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "İşe Başlama Tarihi")]
    public DateOnly? IseBaslamaTarihi { get; set; }

    [StringLength(2000)]
    [Display(Name = "Hakkında")]
    public string? Hakkimda { get; set; }

    public IEnumerable<SelectListItem> DepartmanSecenekleri { get; set; } = [];
}

public class StajyerOlusturViewModel
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(64)]
    [Display(Name = "Ad")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(64)]
    [Display(Name = "Soyad")]
    public string Soyad { get; set; } = string.Empty;

    // Yalnızca geçici parola üretiminde kullanılır; SAKLANMAZ ve loglanmaz.
    [Required(ErrorMessage = "T.C. Kimlik No zorunludur.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "T.C. Kimlik No 11 haneli rakam olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "T.C. Kimlik No (geçici parola olur)")]
    public string TcKimlikNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kart numarası zorunludur.")]
    [StringLength(64)]
    [Display(Name = "RFID Kart No")]
    public string KartNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Staj başlangıcı zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Staj Başlangıcı")]
    public DateOnly? StajBaslangic { get; set; }

    [Required(ErrorMessage = "Staj bitişi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Staj Bitişi")]
    public DateOnly? StajBitis { get; set; }

    [StringLength(256)]
    [Display(Name = "Okul")]
    public string? Okul { get; set; }

    [StringLength(256)]
    [Display(Name = "Bölüm")]
    public string? Bolum { get; set; }

    [Display(Name = "Amir (departman amirden gelir)")]
    public int? AmirProfilId { get; set; }

    public IEnumerable<SelectListItem> AmirSecenekleri { get; set; } = [];
}

public class EslestirViewModel
{
    public int StajyerProfilId { get; set; }
    public string StajyerAdSoyad { get; set; } = string.Empty;

    [Display(Name = "Amir")]
    public int? AmirProfilId { get; set; }

    public IEnumerable<SelectListItem> AmirSecenekleri { get; set; } = [];
}
