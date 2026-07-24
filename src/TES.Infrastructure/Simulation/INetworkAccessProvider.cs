namespace TES.Infrastructure.Simulation;

/// <summary>
/// Misafir ağı enforcement soyutlaması. Gerçek ağ erişimi YALNIZCA bu arayüz üzerinden
/// verilir/kapatılır; uygulama hiçbir yerde doğrudan firewall/RADIUS çağırmaz
/// (CLAUDE.md Bölüm 3 ve 13).
/// </summary>
public interface INetworkAccessProvider
{
    /// <summary>Talep için ağ erişimini verilen bitiş zamanına kadar açar.</summary>
    Task ErisimVerAsync(int talepId, DateTime bitis);

    /// <summary>Talebin ağ erişimini kapatır (süre dolumu veya iptal).</summary>
    Task ErisimKapatAsync(int talepId);

    /// <summary>
    /// Voucher doğrulaması SONRASI yeni cihazı ağa kaydeder.
    /// Erişim aktif değilse veya cihaz limiti dolduysa false döner.
    /// </summary>
    Task<(bool Basarili, int CihazSayisi)> CihazKaydetAsync(int talepId, int cihazLimiti);

    /// <summary>Enforcement katmanındaki güncel durumu okur (izleme ekranları için).</summary>
    Task<SimuleAgErisimi?> DurumGetirAsync(int talepId);
}
