namespace TES.Domain.Entities;

/// <summary>Gönderinin moderasyon durumu (CLAUDE.md Bölüm 6).</summary>
public enum ModerasyonDurumu
{
    Beklemede = 0,
    Onaylandi = 1,
    Reddedildi = 2
}

/// <summary>
/// Kurum içi sosyal platform gönderisi. Kural (CLAUDE.md Bölüm 5): stajyer gönderileri
/// admin onayına tabidir (Beklemede); amir/admin gönderileri onaysız yayınlanır (Onaylandi).
/// Onaylanmayan gönderi akışta yalnızca sahibine görünür.
/// </summary>
public class Gonderi
{
    public int Id { get; set; }

    /// <summary>Yazarın Identity kullanıcı Id'si (AspNetUsers.Id).</summary>
    public required string YazarId { get; set; }

    public required string Icerik { get; set; }

    public ModerasyonDurumu ModerasyonDurumu { get; set; } = ModerasyonDurumu.Beklemede;

    /// <summary>Reddedildiyse moderatörün gerekçesi (sahibine gösterilir).</summary>
    public string? RedMesaji { get; set; }

    public DateTime OlusturmaZamani { get; set; } = DateTime.UtcNow;

    public DateTime? ModerasyonZamani { get; set; }

    /// <summary>Kararı veren moderatörün kullanıcı adı (denetim izi için).</summary>
    public string? ModeratorAdi { get; set; }

    public ICollection<Yorum> Yorumlar { get; set; } = [];
    public ICollection<Begeni> Begeniler { get; set; } = [];
}
