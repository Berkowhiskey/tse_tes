namespace TES.Infrastructure.Simulation;

public enum SimuleAgDurumu
{
    Pending = 0,
    Enabled = 1,
    Expired = 2
}

/// <summary>
/// Simüle firewall'un (enforcement katmanı) kendi durum tablosu. Karar katmanındaki
/// MisafirErisimTalebi.Durum'dan AYRI tutulur: uygulama yalnızca karar verir, paketlere
/// gerçekten izin veren katman bu tablodur (CLAUDE.md Bölüm 3 — iki katman ayrımı).
/// GERÇEKTE: bu tablo var olmaz; durum FortiGate/RADIUS tarafında yaşar.
/// </summary>
public class SimuleAgErisimi
{
    public int Id { get; set; }

    /// <summary>İlgili misafir talebinin Id'si (talep başına tek kayıt).</summary>
    public int MisafirErisimTalebiId { get; set; }

    public SimuleAgDurumu Durum { get; set; } = SimuleAgDurumu.Pending;

    /// <summary>Erişimin biteceği an.</summary>
    public DateTime? Bitis { get; set; }

    /// <summary>Voucher ile eklenen cihaz sayısı (cihaz limiti karar katmanında denetlenir).</summary>
    public int CihazSayisi { get; set; }

    public DateTime GuncellemeZamani { get; set; } = DateTime.UtcNow;
}
