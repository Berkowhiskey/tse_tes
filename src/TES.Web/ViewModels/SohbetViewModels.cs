using TES.Domain.Entities;
using TES.Infrastructure.Services;

namespace TES.Web.ViewModels;

public class SohbetIndexViewModel
{
    public IReadOnlyList<KonusmaKisi> Konusmalar { get; set; } = [];
    public IReadOnlyList<KonusmaCifti> AdminKonusmalari { get; set; } = [];
    public bool AdminMi { get; set; }
}

public class SohbetPencereViewModel
{
    public string BenId { get; set; } = string.Empty;
    public string KarsiId { get; set; } = string.Empty;
    public string KarsiAdSoyad { get; set; } = string.Empty;
    public IReadOnlyList<SohbetMesaji> Mesajlar { get; set; } = [];

    /// <summary>Admin izleme modunda mesaj kutusu gizlenir (read-only).</summary>
    public bool SaltOkunur { get; set; }
}
