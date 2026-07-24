using Microsoft.EntityFrameworkCore;
using TES.Domain.Entities;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Services;

public class OdevServisi(TesDbContext db, IDenetimServisi denetim) : IOdevServisi
{
    public async Task<IReadOnlyList<Odev>> StajyerinOdevleriAsync(int stajyerProfilId)
    {
        return await db.Odevler
            .AsNoTracking()
            .Where(o => o.StajyerProfilId == stajyerProfilId)
            .OrderByDescending(o => o.OlusturmaZamani)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Odev>> KendiOdevlerimAsync(string kullaniciId)
    {
        return await db.Odevler
            .AsNoTracking()
            .Where(o => o.StajyerProfil!.KullaniciId == kullaniciId)
            .OrderByDescending(o => o.OlusturmaZamani)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Odev>> AmirinAttiklariAsync(string amirKullaniciId)
    {
        return await db.Odevler
            .AsNoTracking()
            .Include(o => o.StajyerProfil)
            .Where(o => o.AmirProfil!.KullaniciId == amirKullaniciId)
            .OrderByDescending(o => o.OlusturmaZamani)
            .ToListAsync();
    }

    public Task<Odev?> GetirAsync(int odevId) =>
        db.Odevler.AsNoTracking().FirstOrDefaultAsync(o => o.Id == odevId);

    public async Task<IsSonucu> AtaAsync(int stajyerProfilId, string baslik, string? aciklama, DateOnly? teslimTarihi, YetkiBaglami yetki)
    {
        var stajyer = await db.StajyerProfilleri.FirstOrDefaultAsync(s => s.Id == stajyerProfilId);
        if (stajyer is null)
            return IsSonucu.Hata_("Stajyer bulunamadı.");

        if (stajyer.AmirId is null)
            return IsSonucu.Hata_("Ödev atamak için stajyerin bir amiri olmalı. Önce eşleştirme yapın.");

        // Yalnızca Admin veya stajyerin amiri atayabilir.
        if (!await AmirVeyaAdminMiAsync(stajyer, yetki))
            return IsSonucu.Hata_("Bu işlem için yetkiniz yok.");

        db.Odevler.Add(new Odev
        {
            StajyerProfilId = stajyerProfilId,
            AmirProfilId = stajyer.AmirId.Value, // ödev, stajyerin amiri adına kaydedilir
            Baslik = baslik.Trim(),
            Aciklama = aciklama?.Trim(),
            TeslimTarihi = teslimTarihi
        });

        await db.SaveChangesAsync();
        await denetim.KaydetAsync(yetki.KullaniciId, "OdevAtandi", $"StajyerProfil: {stajyerProfilId}");
        return IsSonucu.Basari;
    }

    public async Task<IsSonucu> DurumGuncelleAsync(int odevId, int ilerleme, IsDurumu durum, YetkiBaglami yetki)
    {
        if (ilerleme is < 0 or > 100)
            return IsSonucu.Hata_("İlerleme 0-100 aralığında olmalıdır.");

        var odev = await db.Odevler.Include(o => o.StajyerProfil).FirstOrDefaultAsync(o => o.Id == odevId);
        if (odev?.StajyerProfil is null)
            return IsSonucu.Hata_("Ödev bulunamadı.");

        // Admin, atayan amir veya ödevin sahibi stajyer güncelleyebilir.
        var sahipStajyer = odev.StajyerProfil.KullaniciId == yetki.KullaniciId;
        if (!sahipStajyer && !await AmirVeyaAdminMiAsync(odev.StajyerProfil, yetki))
            return IsSonucu.Hata_("Bu işlem için yetkiniz yok.");

        odev.Ilerleme = ilerleme;
        odev.Durum = ilerleme == 100 ? IsDurumu.Tamamlandi : durum;
        odev.GuncellemeZamani = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await denetim.KaydetAsync(yetki.KullaniciId, "OdevDurumGuncellendi", $"Odev: {odevId}, %{ilerleme}");
        return IsSonucu.Basari;
    }

    public async Task<IsSonucu> SilAsync(int odevId, YetkiBaglami yetki)
    {
        var odev = await db.Odevler.Include(o => o.StajyerProfil).FirstOrDefaultAsync(o => o.Id == odevId);
        if (odev?.StajyerProfil is null)
            return IsSonucu.Hata_("Ödev bulunamadı.");

        if (!await AmirVeyaAdminMiAsync(odev.StajyerProfil, yetki))
            return IsSonucu.Hata_("Bu işlem için yetkiniz yok.");

        db.Odevler.Remove(odev);
        await db.SaveChangesAsync();
        await denetim.KaydetAsync(yetki.KullaniciId, "OdevSilindi", $"Odev: {odevId}");
        return IsSonucu.Basari;
    }

    private async Task<bool> AmirVeyaAdminMiAsync(StajyerProfil stajyer, YetkiBaglami yetki)
    {
        if (yetki.IsAdmin)
            return true;

        if (!yetki.IsAmir || stajyer.AmirId is null)
            return false;

        return await db.AmirProfilleri.AnyAsync(a => a.Id == stajyer.AmirId && a.KullaniciId == yetki.KullaniciId);
    }
}
