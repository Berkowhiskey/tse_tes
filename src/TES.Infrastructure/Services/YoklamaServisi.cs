using Microsoft.EntityFrameworkCore;
using TES.Domain.Entities;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Services;

public class YoklamaServisi(TesDbContext db) : IYoklamaServisi
{
    public async Task<(KartOkutmaSonucu Sonuc, StajyerProfil? Stajyer)> KartOkutAsync(string kartNo, DateTime? zaman = null)
    {
        var simdi = zaman ?? DateTime.Now;

        var stajyer = await db.StajyerProfilleri.FirstOrDefaultAsync(s => s.KartNo == kartNo);
        if (stajyer is null)
            return (KartOkutmaSonucu.KartBulunamadi, null);

        var gunBasi = simdi.Date;
        var gunSonu = gunBasi.AddDays(1);

        // Aynı gün içinde açık oturum var mı? (giriş-çıkış çiftleri gün içinde eşleştirilir)
        var acikOturum = await db.YoklamaKayitlari
            .Where(y => y.StajyerProfilId == stajyer.Id
                        && y.CikisZamani == null
                        && y.GirisZamani >= gunBasi && y.GirisZamani < gunSonu)
            .OrderByDescending(y => y.GirisZamani)
            .FirstOrDefaultAsync();

        if (acikOturum is not null)
        {
            acikOturum.CikisZamani = simdi;
            await db.SaveChangesAsync();
            return (KartOkutmaSonucu.CikisYapildi, stajyer);
        }

        db.YoklamaKayitlari.Add(new YoklamaKaydi
        {
            StajyerProfilId = stajyer.Id,
            GirisZamani = simdi
        });
        await db.SaveChangesAsync();

        return (KartOkutmaSonucu.GirisYapildi, stajyer);
    }

    public async Task<IReadOnlyList<YoklamaKaydi>> KayitlariGetirAsync(IReadOnlyCollection<int> stajyerProfilIdleri, int enFazla = 100)
    {
        return await db.YoklamaKayitlari
            .AsNoTracking()
            .Where(y => stajyerProfilIdleri.Contains(y.StajyerProfilId))
            .Include(y => y.StajyerProfil)
            .OrderByDescending(y => y.GirisZamani)
            .Take(enFazla)
            .ToListAsync();
    }
}
