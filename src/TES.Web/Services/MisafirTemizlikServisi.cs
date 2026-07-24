using TES.Infrastructure.Services;

namespace TES.Web.Services;

/// <summary>
/// Arka plan taraması (60 sn'de bir): sponsor süresinde yanıtlamadıysa talep otomatik
/// iptal edilir; süresi biten Enabled erişimler kapatılır (CLAUDE.md Bölüm 8).
/// </summary>
public class MisafirTemizlikServisi(
    IServiceScopeFactory scopeFactory,
    ILogger<MisafirTemizlikServisi> logger) : BackgroundService
{
    private static readonly TimeSpan Aralik = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var zamanlayici = new PeriodicTimer(Aralik);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var servis = scope.ServiceProvider.GetRequiredService<IMisafirTalepServisi>();

                var islenen = await servis.SuresiGecenleriIsleAsync();
                if (islenen > 0)
                    logger.LogInformation("Misafir temizlik: {Adet} kayıt işlendi.", islenen);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Misafir temizlik taraması başarısız oldu.");
            }

            try
            {
                await zamanlayici.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // uygulama kapanıyor
            }
        }
    }
}
