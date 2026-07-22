using Microsoft.EntityFrameworkCore;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Services;

public class ProfilServisi(TesDbContext db, IDenetimServisi denetim) : IProfilServisi
{
    public async Task<AmirProfilBilgi?> AmirProfilGetirAsync(string kullaniciId)
    {
        return await db.AmirProfilleri
            .AsNoTracking()
            .Where(a => a.KullaniciId == kullaniciId)
            .Select(a => new AmirProfilBilgi(a.Departman!.Ad, a.IseBaslamaTarihi, a.Hakkimda))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> StajyerBilgiGuncelleAsync(string kullaniciId, string? okul, string? bolum, string aktor)
    {
        // Sahiplik: yalnızca kendi profili (kullaniciId oturumdaki kullanıcıdan gelir).
        var profil = await db.StajyerProfilleri.FirstOrDefaultAsync(s => s.KullaniciId == kullaniciId);
        if (profil is null)
            return false;

        profil.Okul = okul?.Trim();
        profil.Bolum = bolum?.Trim();
        await db.SaveChangesAsync();

        await denetim.KaydetAsync(aktor, "ProfilGuncellendi", "Stajyer eğitim bilgileri güncellendi.");
        return true;
    }

    public async Task<bool> AmirHakkimdaGuncelleAsync(string kullaniciId, string? hakkimda, string aktor)
    {
        var profil = await db.AmirProfilleri.FirstOrDefaultAsync(a => a.KullaniciId == kullaniciId);
        if (profil is null)
            return false;

        profil.Hakkimda = hakkimda?.Trim();
        await db.SaveChangesAsync();

        await denetim.KaydetAsync(aktor, "ProfilGuncellendi", "Amir 'Hakkında' bilgisi güncellendi.");
        return true;
    }
}
