namespace TES.Domain.Entities;

/// <summary>Bir gönderiye yapılan yorum. Sahibi veya admin silebilir (CLAUDE.md Bölüm 5).</summary>
public class Yorum
{
    public int Id { get; set; }

    public int GonderiId { get; set; }
    public Gonderi? Gonderi { get; set; }

    /// <summary>Yazarın Identity kullanıcı Id'si (AspNetUsers.Id).</summary>
    public required string YazarId { get; set; }

    public required string Icerik { get; set; }

    public DateTime OlusturmaZamani { get; set; } = DateTime.UtcNow;
}
