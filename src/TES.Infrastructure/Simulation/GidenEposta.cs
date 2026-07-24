namespace TES.Infrastructure.Simulation;

/// <summary>
/// Simüle SMTP'nin "teslimat kanalı": gönderilen e-postalar bu tabloya yazılır ve
/// Admin'in "Giden E-postalar (Simülasyon)" ekranında görünür.
/// GERÇEKTE: bu tablo var olmaz; e-posta kurumun SMTP relay'i üzerinden dışarı gider
/// ve içeriği (token bağlantısı dahil) sistemde saklanmaz.
/// </summary>
public class GidenEposta
{
    public int Id { get; set; }
    public required string Kime { get; set; }
    public required string Konu { get; set; }
    public required string Icerik { get; set; }
    public DateTime Zaman { get; set; } = DateTime.UtcNow;
}
