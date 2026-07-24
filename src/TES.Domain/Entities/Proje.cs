namespace TES.Domain.Entities;

/// <summary>
/// Stajyerin üzerinde çalıştığı proje. Kural (CLAUDE.md Bölüm 6): stajyer başına TEK proje
/// (StajyerProfilId benzersizdir). İleride paylaşımlı senaryo gelirse çoka-çok'a genişletilir.
/// </summary>
public class Proje
{
    public int Id { get; set; }

    public int StajyerProfilId { get; set; }
    public StajyerProfil? StajyerProfil { get; set; }

    public required string Ad { get; set; }

    public string? Aciklama { get; set; }

    public IsDurumu Durum { get; set; } = IsDurumu.Baslamadi;

    /// <summary>Yüzde ilerleme (0-100).</summary>
    public int Ilerleme { get; set; }

    public DateTime OlusturmaZamani { get; set; } = DateTime.UtcNow;

    public DateTime? GuncellemeZamani { get; set; }
}
