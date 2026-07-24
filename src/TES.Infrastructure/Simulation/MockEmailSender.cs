using Microsoft.Extensions.Logging;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Simulation;

// GERÇEKTE: Kurumun kimlik doğrulamalı SMTP relay'i (SPF/DKIM) kullanılır; e-posta
// içerikleri veritabanına yazılmaz. Bu mock, e-postayı GidenEposta tablosuna kaydeder
// ki demo sırasında sponsor onay bağlantısına erişilebilsin (CLAUDE.md Bölüm 9).
public class MockEmailSender(TesDbContext db, ILogger<MockEmailSender> logger) : IEmailSender
{
    public async Task GonderAsync(string kime, string konu, string icerik)
    {
        db.Set<GidenEposta>().Add(new GidenEposta
        {
            Kime = kime,
            Konu = konu,
            Icerik = icerik,
            Zaman = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Loga içerik YAZILMAZ (token/voucher sızmasın); yalnızca gönderim olayı düşülür.
        logger.LogInformation("Simüle e-posta gönderildi: {Kime} — {Konu}", kime, konu);
    }
}
