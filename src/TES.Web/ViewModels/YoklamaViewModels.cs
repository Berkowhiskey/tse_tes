using Microsoft.AspNetCore.Mvc.Rendering;

namespace TES.Web.ViewModels;

public class YoklamaListeViewModel
{
    public IReadOnlyList<YoklamaSatirViewModel> Satirlar { get; set; } = [];
    public string KapsamAciklama { get; set; } = string.Empty;
}

public class YoklamaSatirViewModel
{
    public string StajyerAdSoyad { get; set; } = string.Empty;
    public DateTime GirisZamani { get; set; }
    public DateTime? CikisZamani { get; set; }

    /// <summary>Eşi olmayan giriş "açık oturum" sayılır (CLAUDE.md Bölüm 6).</summary>
    public bool AcikOturum => CikisZamani is null;
}

public class SimulatorViewModel
{
    public IEnumerable<SelectListItem> KartSecenekleri { get; set; } = [];
    public string? SonucMesaji { get; set; }
    public bool? SonucBasarili { get; set; }

    /// <summary>Admin tüm kartları okutabilir; stajyer yalnızca kendi kartını (sunucuda çözülür).</summary>
    public bool AdminMi { get; set; }
    public string? KendiKartNo { get; set; }
    public string? KendiAdSoyad { get; set; }
}
