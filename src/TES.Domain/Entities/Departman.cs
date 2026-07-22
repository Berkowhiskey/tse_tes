namespace TES.Domain.Entities;

/// <summary>
/// Kendine referanslı departman hiyerarşisi.
/// Örn: Bilgi-İşlem → { Yazılım Geliştirme, Sistem, Donanım }.
/// Amirin departmanı = stajyerin departmanı (eşleştirmede amirden gelir).
/// </summary>
public class Departman
{
    public int Id { get; set; }

    public required string Ad { get; set; }

    /// <summary>Üst departman; kök departmanlarda null.</summary>
    public int? UstDepartmanId { get; set; }
    public Departman? UstDepartman { get; set; }

    public ICollection<Departman> AltDepartmanlar { get; set; } = [];
}
