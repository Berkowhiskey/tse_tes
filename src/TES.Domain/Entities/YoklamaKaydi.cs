namespace TES.Domain.Entities;

/// <summary>
/// Giriş-çıkış kaydı. Kural (CLAUDE.md Bölüm 6): aynı gün içindeki giriş-çıkış çiftleri
/// eşleştirilir; eşi olmayan giriş (CikisZamani == null) "açık oturum" sayılır.
/// </summary>
public class YoklamaKaydi
{
    public int Id { get; set; }

    public int StajyerProfilId { get; set; }
    public StajyerProfil? StajyerProfil { get; set; }

    public DateTime GirisZamani { get; set; }

    /// <summary>Null ise oturum hâlâ açıktır.</summary>
    public DateTime? CikisZamani { get; set; }
}
