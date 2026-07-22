namespace TES.Domain.Entities;

/// <summary>
/// Amir rolündeki kullanıcının profil bilgileri.
/// Kimlik (parola/rol) tarafı Identity'dedir; burada yalnızca alan bilgisi tutulur.
/// </summary>
public class AmirProfil
{
    public int Id { get; set; }

    /// <summary>Identity kullanıcısına bağ (AspNetUsers.Id).</summary>
    public required string KullaniciId { get; set; }

    public DateOnly? IseBaslamaTarihi { get; set; }

    public string? Hakkimda { get; set; }

    public int DepartmanId { get; set; }
    public Departman? Departman { get; set; }

    public ICollection<StajyerProfil> Stajyerler { get; set; } = [];
}
