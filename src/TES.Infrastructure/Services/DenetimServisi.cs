using TES.Domain.Entities;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Services;

public class DenetimServisi(TesDbContext db) : IDenetimServisi
{
    public async Task KaydetAsync(string aktor, string islem, string? detay = null)
    {
        db.DenetimKayitlari.Add(new DenetimKaydi
        {
            Aktor = aktor,
            Islem = islem,
            Detay = detay,
            Zaman = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
