namespace TES.Domain.Entities;

/// <summary>
/// Amirin stajyerine atadığı ödev (CLAUDE.md Bölüm 6: AmirId → StajyerId).
/// Bir stajyerin birden fazla ödevi olabilir; ödevi atayan amir kaydı tutulur.
/// </summary>
public class Odev
{
    public int Id { get; set; }

    public int AmirProfilId { get; set; }
    public AmirProfil? AmirProfil { get; set; }

    public int StajyerProfilId { get; set; }
    public StajyerProfil? StajyerProfil { get; set; }

    public required string Baslik { get; set; }

    public string? Aciklama { get; set; }

    public IsDurumu Durum { get; set; } = IsDurumu.Baslamadi;

    /// <summary>Yüzde ilerleme (0-100).</summary>
    public int Ilerleme { get; set; }

    public DateOnly? TeslimTarihi { get; set; }

    public DateTime OlusturmaZamani { get; set; } = DateTime.UtcNow;

    public DateTime? GuncellemeZamani { get; set; }
}
