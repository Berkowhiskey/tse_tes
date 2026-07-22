using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TES.Web.ViewModels;

public class DepartmanListeViewModel
{
    public IReadOnlyList<DepartmanSatirViewModel> Satirlar { get; set; } = [];
}

public class DepartmanSatirViewModel
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int Seviye { get; set; }
}

public class DepartmanFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Departman adı zorunludur.")]
    [StringLength(128)]
    [Display(Name = "Departman Adı")]
    public string Ad { get; set; } = string.Empty;

    [Display(Name = "Üst Departman")]
    public int? UstDepartmanId { get; set; }

    public IEnumerable<SelectListItem> DepartmanSecenekleri { get; set; } = [];
}
