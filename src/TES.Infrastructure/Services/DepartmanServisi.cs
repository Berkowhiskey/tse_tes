using Microsoft.EntityFrameworkCore;
using TES.Domain.Entities;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Services;

public class DepartmanServisi(TesDbContext db, IDenetimServisi denetim) : IDepartmanServisi
{
    public async Task<IReadOnlyList<(Departman Departman, int Seviye)>> HiyerarsikListeAsync()
    {
        var hepsi = await db.Departmanlar
            .AsNoTracking()
            .OrderBy(d => d.Ad)
            .ToListAsync();

        var cocuklar = hepsi.ToLookup(d => d.UstDepartmanId);
        var sonuc = new List<(Departman, int)>();

        void Ekle(int? ustId, int seviye)
        {
            foreach (var d in cocuklar[ustId])
            {
                sonuc.Add((d, seviye));
                Ekle(d.Id, seviye + 1);
            }
        }

        Ekle(null, 0);
        return sonuc;
    }

    public Task<Departman?> GetirAsync(int id) =>
        db.Departmanlar.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);

    public async Task<Departman> OlusturAsync(string ad, int? ustDepartmanId, string aktor)
    {
        var departman = new Departman { Ad = ad.Trim(), UstDepartmanId = ustDepartmanId };
        db.Departmanlar.Add(departman);
        await db.SaveChangesAsync();

        await denetim.KaydetAsync(aktor, "DepartmanOlusturuldu", $"Departman: {departman.Ad} (Id={departman.Id})");
        return departman;
    }

    public async Task GuncelleAsync(int id, string ad, int? ustDepartmanId, string aktor)
    {
        var departman = await db.Departmanlar.FirstAsync(d => d.Id == id);

        // Basit döngü koruması: kendisi veya kendi alt ağacı üst departman yapılamaz.
        if (ustDepartmanId == id || await AltAgacindaMiAsync(id, ustDepartmanId))
            throw new InvalidOperationException("Bir departman kendi alt ağacına taşınamaz.");

        departman.Ad = ad.Trim();
        departman.UstDepartmanId = ustDepartmanId;
        await db.SaveChangesAsync();

        await denetim.KaydetAsync(aktor, "DepartmanGuncellendi", $"Departman: {departman.Ad} (Id={id})");
    }

    public async Task<(bool Basarili, string? Hata)> SilAsync(int id, string aktor)
    {
        var departman = await db.Departmanlar.FirstOrDefaultAsync(d => d.Id == id);
        if (departman is null)
            return (false, "Departman bulunamadı.");

        if (await db.Departmanlar.AnyAsync(d => d.UstDepartmanId == id))
            return (false, "Alt departmanı olan departman silinemez.");

        if (await db.AmirProfilleri.AnyAsync(a => a.DepartmanId == id) ||
            await db.StajyerProfilleri.AnyAsync(s => s.DepartmanId == id))
            return (false, "Amiri veya stajyeri olan departman silinemez.");

        db.Departmanlar.Remove(departman);
        await db.SaveChangesAsync();

        await denetim.KaydetAsync(aktor, "DepartmanSilindi", $"Departman: {departman.Ad} (Id={id})");
        return (true, null);
    }

    private async Task<bool> AltAgacindaMiAsync(int kokId, int? adayUstId)
    {
        var gezilen = adayUstId;
        while (gezilen is not null)
        {
            if (gezilen == kokId)
                return true;

            gezilen = await db.Departmanlar
                .Where(d => d.Id == gezilen)
                .Select(d => d.UstDepartmanId)
                .FirstOrDefaultAsync();
        }

        return false;
    }
}
