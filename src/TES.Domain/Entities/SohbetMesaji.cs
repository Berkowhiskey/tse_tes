namespace TES.Domain.Entities;

/// <summary>
/// Amir ↔ stajyer arasındaki tekil sohbet mesajı (CLAUDE.md Bölüm 6).
/// Konuşma izni (kimin kiminle konuşabileceği) servis/hub katmanında sunucuda doğrulanır.
/// </summary>
public class SohbetMesaji
{
    public int Id { get; set; }

    /// <summary>Gönderenin Identity kullanıcı Id'si (AspNetUsers.Id).</summary>
    public required string GondericiId { get; set; }

    /// <summary>Alıcının Identity kullanıcı Id'si (AspNetUsers.Id).</summary>
    public required string AliciId { get; set; }

    public required string Icerik { get; set; }

    public DateTime Zaman { get; set; } = DateTime.UtcNow;

    /// <summary>Alıcı tarafından okunma durumu (okunmamış bildirim sayacı için).</summary>
    public bool OkunduMu { get; set; }
}
