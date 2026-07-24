using Microsoft.EntityFrameworkCore;
using TES.Domain.Entities;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Services;

public class ProjeServisi(TesDbContext db, IDenetimServisi denetim) : IProjeServisi
{
    public Task<Proje?> StajyerinProjesiAsync(int stajyerProfilId) =>
        db.Projeler.AsNoTracking().FirstOrDefaultAsync(p => p.StajyerProfilId == stajyerProfilId);

    public async Task<Proje?> KendiProjemAsync(string kullaniciId)
    {
        var stajyerId = await db.StajyerProfilleri
            .Where(s => s.KullaniciId == kullaniciId)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        return stajyerId is null ? null : await StajyerinProjesiAsync(stajyerId.Value);
    }

    public async Task<IsSonucu> KaydetAsync(int stajyerProfilId, string ad, string? aciklama, YetkiBaglami yetki)
    {
        var stajyer = await db.StajyerProfilleri.FirstOrDefaultAsync(s => s.Id == stajyerProfilId);
        if (stajyer is null)
            return IsSonucu.Hata_("Stajyer bulunamadı.");

        // Yalnızca Admin veya bu stajyerin amiri proje atayabilir/düzenleyebilir.
        if (!await AmirVeyaAdminMiAsync(stajyer, yetki))
            return IsSonucu.Hata_("Bu işlem için yetkiniz yok.");

        var proje = await db.Projeler.FirstOrDefaultAsync(p => p.StajyerProfilId == stajyerProfilId);

        if (proje is null)
        {
            proje = new Proje { StajyerProfilId = stajyerProfilId, Ad = ad.Trim(), Aciklama = aciklama?.Trim() };
            db.Projeler.Add(proje);
        }
        else
        {
            proje.Ad = ad.Trim();
            proje.Aciklama = aciklama?.Trim();
            proje.GuncellemeZamani = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        await denetim.KaydetAsync(yetki.KullaniciId, "ProjeKaydedildi", $"StajyerProfil: {stajyerProfilId}");
        return IsSonucu.Basari;
    }

    public async Task<IsSonucu> IlerlemeGuncelleAsync(int projeId, int ilerleme, IsDurumu durum, YetkiBaglami yetki)
    {
        if (ilerleme is < 0 or > 100)
            return IsSonucu.Hata_("İlerleme 0-100 aralığında olmalıdır.");

        var proje = await db.Projeler.Include(p => p.StajyerProfil).FirstOrDefaultAsync(p => p.Id == projeId);
        if (proje?.StajyerProfil is null)
            return IsSonucu.Hata_("Proje bulunamadı.");

        // Admin, stajyerin amiri veya projenin sahibi stajyer güncelleyebilir.
        var sahipStajyer = proje.StajyerProfil.KullaniciId == yetki.KullaniciId;
        if (!sahipStajyer && !await AmirVeyaAdminMiAsync(proje.StajyerProfil, yetki))
            return IsSonucu.Hata_("Bu işlem için yetkiniz yok.");

        proje.Ilerleme = ilerleme;
        proje.Durum = durum;
        // İlerleme 100 ise durumu tutarlı biçimde tamamlanmış say.
        if (ilerleme == 100)
            proje.Durum = IsDurumu.Tamamlandi;
        proje.GuncellemeZamani = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await denetim.KaydetAsync(yetki.KullaniciId, "ProjeIlerlemeGuncellendi", $"Proje: {projeId}, %{ilerleme}");
        return IsSonucu.Basari;
    }

    public async Task<IsSonucu> SilAsync(int projeId, YetkiBaglami yetki)
    {
        var proje = await db.Projeler.Include(p => p.StajyerProfil).FirstOrDefaultAsync(p => p.Id == projeId);
        if (proje?.StajyerProfil is null)
            return IsSonucu.Hata_("Proje bulunamadı.");

        if (!await AmirVeyaAdminMiAsync(proje.StajyerProfil, yetki))
            return IsSonucu.Hata_("Bu işlem için yetkiniz yok.");

        db.Projeler.Remove(proje);
        await db.SaveChangesAsync();
        await denetim.KaydetAsync(yetki.KullaniciId, "ProjeSilindi", $"Proje: {projeId}");
        return IsSonucu.Basari;
    }

    /// <summary>Admin her stajyeri; amir yalnızca kendi stajyerini yönetebilir.</summary>
    private async Task<bool> AmirVeyaAdminMiAsync(StajyerProfil stajyer, YetkiBaglami yetki)
    {
        if (yetki.IsAdmin)
            return true;

        if (!yetki.IsAmir || stajyer.AmirId is null)
            return false;

        return await db.AmirProfilleri.AnyAsync(a => a.Id == stajyer.AmirId && a.KullaniciId == yetki.KullaniciId);
    }
}
