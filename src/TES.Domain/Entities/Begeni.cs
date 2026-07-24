namespace TES.Domain.Entities;

/// <summary>
/// Bir kullanıcının bir gönderiyi beğenmesi. (GonderiId, KullaniciId) çifti benzersizdir —
/// aynı kullanıcı bir gönderiyi bir kez beğenir (toggle).
/// </summary>
public class Begeni
{
    public int Id { get; set; }

    public int GonderiId { get; set; }
    public Gonderi? Gonderi { get; set; }

    /// <summary>Beğenen kullanıcının Identity Id'si (AspNetUsers.Id).</summary>
    public required string KullaniciId { get; set; }

    public DateTime OlusturmaZamani { get; set; } = DateTime.UtcNow;
}
