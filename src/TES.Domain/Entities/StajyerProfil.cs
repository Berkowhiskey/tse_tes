namespace TES.Domain.Entities;

/// <summary>
/// Stajyerin kimlik/eğitim ve staj bilgileri. Tek amiri vardır; departmanı amirden gelir.
/// NOT (KVKK): T.C. Kimlik No burada SAKLANMAZ — yalnızca ilk geçici parola olarak
/// kullanılır ve Identity tarafından hash'lenir. Hassas alan sertleştirmesi ileriki fazda.
/// </summary>
public class StajyerProfil
{
    public int Id { get; set; }

    /// <summary>Identity kullanıcısına bağ (AspNetUsers.Id).</summary>
    public required string KullaniciId { get; set; }

    /// <summary>RFID kart numarası (yoklama eşleşmesi için benzersiz).</summary>
    public required string KartNo { get; set; }

    public string? Okul { get; set; }
    public string? Bolum { get; set; }

    public DateOnly StajBaslangic { get; set; }
    public DateOnly StajBitis { get; set; }

    /// <summary>Tek amir kuralı; eşleştirme yapılana kadar null olabilir.</summary>
    public int? AmirId { get; set; }
    public AmirProfil? Amir { get; set; }

    /// <summary>Amirden gelir; amir atanmadan null'dır.</summary>
    public int? DepartmanId { get; set; }
    public Departman? Departman { get; set; }

    public ICollection<YoklamaKaydi> YoklamaKayitlari { get; set; } = [];
}
