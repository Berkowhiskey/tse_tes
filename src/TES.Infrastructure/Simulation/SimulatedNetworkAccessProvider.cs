using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Simulation;

// GERÇEKTE: FortiGate/Firewall guest portal API'si + RADIUS CoA çağrılır; misafir MAC/oturumu
// firewall'da yetkilendirilir. Bu simülasyon, enforcement durumunu SimuleAgErisimi tablosunda
// Pending → Enabled → Expired olarak yürütür (CLAUDE.md Bölüm 9).
public class SimulatedNetworkAccessProvider(
    TesDbContext db,
    ILogger<SimulatedNetworkAccessProvider> logger) : INetworkAccessProvider
{
    public async Task ErisimVerAsync(int talepId, DateTime bitis)
    {
        var kayit = await KayitGetirVeyaOlusturAsync(talepId);

        kayit.Durum = SimuleAgDurumu.Enabled;
        kayit.Bitis = bitis;
        kayit.GuncellemeZamani = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("Simüle ağ erişimi AÇILDI: Talep {TalepId}, bitiş {Bitis}", talepId, bitis);
    }

    public async Task ErisimKapatAsync(int talepId)
    {
        var kayit = await KayitGetirVeyaOlusturAsync(talepId);

        kayit.Durum = SimuleAgDurumu.Expired;
        kayit.GuncellemeZamani = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("Simüle ağ erişimi KAPATILDI: Talep {TalepId}", talepId);
    }

    public async Task<(bool Basarili, int CihazSayisi)> CihazKaydetAsync(int talepId, int cihazLimiti)
    {
        var kayit = await db.Set<SimuleAgErisimi>()
            .FirstOrDefaultAsync(k => k.MisafirErisimTalebiId == talepId);

        if (kayit is null || kayit.Durum != SimuleAgDurumu.Enabled ||
            (kayit.Bitis is not null && kayit.Bitis < DateTime.UtcNow))
            return (false, kayit?.CihazSayisi ?? 0);

        if (kayit.CihazSayisi >= cihazLimiti)
            return (false, kayit.CihazSayisi);

        kayit.CihazSayisi++;
        kayit.GuncellemeZamani = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("Simüle ağa cihaz eklendi: Talep {TalepId}, cihaz {Sayi}", talepId, kayit.CihazSayisi);
        return (true, kayit.CihazSayisi);
    }

    public Task<SimuleAgErisimi?> DurumGetirAsync(int talepId) =>
        db.Set<SimuleAgErisimi>()
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.MisafirErisimTalebiId == talepId);

    private async Task<SimuleAgErisimi> KayitGetirVeyaOlusturAsync(int talepId)
    {
        var kayit = await db.Set<SimuleAgErisimi>()
            .FirstOrDefaultAsync(k => k.MisafirErisimTalebiId == talepId);

        if (kayit is null)
        {
            kayit = new SimuleAgErisimi { MisafirErisimTalebiId = talepId };
            db.Set<SimuleAgErisimi>().Add(kayit);
        }

        return kayit;
    }
}
