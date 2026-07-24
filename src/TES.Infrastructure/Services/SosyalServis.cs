using Microsoft.EntityFrameworkCore;
using TES.Domain.Entities;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Services;

/// <summary>
/// Sosyal platform: gönderi + moderasyon + yorum + beğeni.
/// Moderasyon kuralı (CLAUDE.md Bölüm 5, 13): stajyer gönderisi onaysız YAYINA ALINMAZ;
/// amir/admin gönderisi onaydan muaftır. İçerik kaldırma sahiplik/rol ile sunucuda denetlenir.
/// </summary>
public class SosyalServis(TesDbContext db, IDenetimServisi denetim) : ISosyalServis
{
    public async Task<IReadOnlyList<GonderiOzeti>> FeedGetirAsync(YetkiBaglami yetki)
    {
        // Onaylanmış her gönderi + isteğin sahibinin kendi gönderileri (durumu ne olursa olsun).
        var gonderiler = await db.Gonderiler
            .AsNoTracking()
            .Where(g => g.ModerasyonDurumu == ModerasyonDurumu.Onaylandi || g.YazarId == yetki.KullaniciId)
            .OrderByDescending(g => g.OlusturmaZamani)
            .ToListAsync();

        return await OzetleAsync(gonderiler, yetki);
    }

    public async Task<ModerasyonDurumu> GonderiOlusturAsync(string icerik, YetkiBaglami yetki)
    {
        // Stajyer → Beklemede; Amir/Admin → doğrudan Onaylandi.
        var durum = yetki.IsStajyer && !yetki.IsAmir && !yetki.IsAdmin
            ? ModerasyonDurumu.Beklemede
            : ModerasyonDurumu.Onaylandi;

        db.Gonderiler.Add(new Gonderi
        {
            YazarId = yetki.KullaniciId,
            Icerik = icerik.Trim(),
            ModerasyonDurumu = durum,
            ModerasyonZamani = durum == ModerasyonDurumu.Onaylandi ? DateTime.UtcNow : null
        });

        await db.SaveChangesAsync();
        return durum;
    }

    public async Task<IsSonucu> GonderiSilAsync(int gonderiId, YetkiBaglami yetki)
    {
        var gonderi = await db.Gonderiler.FirstOrDefaultAsync(g => g.Id == gonderiId);
        if (gonderi is null)
            return IsSonucu.Hata_("Gönderi bulunamadı.");

        // Admin herkesin gönderisini; diğerleri yalnızca kendisininkini kaldırabilir.
        if (!yetki.IsAdmin && gonderi.YazarId != yetki.KullaniciId)
            return IsSonucu.Hata_("Bu gönderiyi kaldırma yetkiniz yok.");

        db.Gonderiler.Remove(gonderi);
        await db.SaveChangesAsync();

        if (yetki.IsAdmin && gonderi.YazarId != yetki.KullaniciId)
            await denetim.KaydetAsync(yetki.KullaniciId, "GonderiModerasyonSilme", $"Gonderi: {gonderiId}");

        return IsSonucu.Basari;
    }

    public async Task<IReadOnlyList<GonderiOzeti>> ModerasyonBekleyenlerAsync()
    {
        var bekleyenler = await db.Gonderiler
            .AsNoTracking()
            .Where(g => g.ModerasyonDurumu == ModerasyonDurumu.Beklemede)
            .OrderBy(g => g.OlusturmaZamani)
            .ToListAsync();

        // Moderasyon ekranında silme butonu göstermeyiz; yorumlar da gerekmez.
        var yazarAdlari = await YazarAdlariAsync(bekleyenler.Select(g => g.YazarId));

        return bekleyenler
            .Select(g => new GonderiOzeti(g, yazarAdlari.GetValueOrDefault(g.YazarId, "?"), 0, false, false, []))
            .ToList();
    }

    public async Task<IsSonucu> OnaylaAsync(int gonderiId, string adminAdi)
    {
        var gonderi = await db.Gonderiler.FirstOrDefaultAsync(g => g.Id == gonderiId);
        if (gonderi is null)
            return IsSonucu.Hata_("Gönderi bulunamadı.");

        gonderi.ModerasyonDurumu = ModerasyonDurumu.Onaylandi;
        gonderi.RedMesaji = null;
        gonderi.ModerasyonZamani = DateTime.UtcNow;
        gonderi.ModeratorAdi = adminAdi;
        await db.SaveChangesAsync();

        await denetim.KaydetAsync(adminAdi, "GonderiOnaylandi", $"Gonderi: {gonderiId}");
        return IsSonucu.Basari;
    }

    public async Task<IsSonucu> ReddetAsync(int gonderiId, string redMesaji, string adminAdi)
    {
        if (string.IsNullOrWhiteSpace(redMesaji))
            return IsSonucu.Hata_("Red mesajı zorunludur.");

        var gonderi = await db.Gonderiler.FirstOrDefaultAsync(g => g.Id == gonderiId);
        if (gonderi is null)
            return IsSonucu.Hata_("Gönderi bulunamadı.");

        gonderi.ModerasyonDurumu = ModerasyonDurumu.Reddedildi;
        gonderi.RedMesaji = redMesaji.Trim();
        gonderi.ModerasyonZamani = DateTime.UtcNow;
        gonderi.ModeratorAdi = adminAdi;
        await db.SaveChangesAsync();

        await denetim.KaydetAsync(adminAdi, "GonderiReddedildi", $"Gonderi: {gonderiId}");
        return IsSonucu.Basari;
    }

    public async Task<IsSonucu> YorumEkleAsync(int gonderiId, string icerik, YetkiBaglami yetki)
    {
        if (string.IsNullOrWhiteSpace(icerik))
            return IsSonucu.Hata_("Yorum boş olamaz.");

        // Yorum yalnızca yayında (onaylanmış) gönderilere yapılabilir.
        var onayli = await db.Gonderiler
            .AnyAsync(g => g.Id == gonderiId && g.ModerasyonDurumu == ModerasyonDurumu.Onaylandi);
        if (!onayli)
            return IsSonucu.Hata_("Gönderi bulunamadı veya yayında değil.");

        db.Yorumlar.Add(new Yorum { GonderiId = gonderiId, YazarId = yetki.KullaniciId, Icerik = icerik.Trim() });
        await db.SaveChangesAsync();
        return IsSonucu.Basari;
    }

    public async Task<IsSonucu> YorumSilAsync(int yorumId, YetkiBaglami yetki)
    {
        var yorum = await db.Yorumlar.FirstOrDefaultAsync(y => y.Id == yorumId);
        if (yorum is null)
            return IsSonucu.Hata_("Yorum bulunamadı.");

        if (!yetki.IsAdmin && yorum.YazarId != yetki.KullaniciId)
            return IsSonucu.Hata_("Bu yorumu kaldırma yetkiniz yok.");

        db.Yorumlar.Remove(yorum);
        await db.SaveChangesAsync();

        if (yetki.IsAdmin && yorum.YazarId != yetki.KullaniciId)
            await denetim.KaydetAsync(yetki.KullaniciId, "YorumModerasyonSilme", $"Yorum: {yorumId}");

        return IsSonucu.Basari;
    }

    public async Task<(bool Basarili, bool Begenildi, string? Hata)> BegeniToggleAsync(int gonderiId, YetkiBaglami yetki)
    {
        var onayli = await db.Gonderiler
            .AnyAsync(g => g.Id == gonderiId && g.ModerasyonDurumu == ModerasyonDurumu.Onaylandi);
        if (!onayli)
            return (false, false, "Gönderi bulunamadı veya yayında değil.");

        var mevcut = await db.Begeniler
            .FirstOrDefaultAsync(b => b.GonderiId == gonderiId && b.KullaniciId == yetki.KullaniciId);

        if (mevcut is null)
        {
            db.Begeniler.Add(new Begeni { GonderiId = gonderiId, KullaniciId = yetki.KullaniciId });
            await db.SaveChangesAsync();
            return (true, true, null);
        }

        db.Begeniler.Remove(mevcut);
        await db.SaveChangesAsync();
        return (true, false, null);
    }

    // ---- yardımcılar ----

    private async Task<IReadOnlyList<GonderiOzeti>> OzetleAsync(List<Gonderi> gonderiler, YetkiBaglami yetki)
    {
        if (gonderiler.Count == 0)
            return [];

        var gonderiIdleri = gonderiler.Select(g => g.Id).ToList();

        var yorumlar = await db.Yorumlar
            .AsNoTracking()
            .Where(y => gonderiIdleri.Contains(y.GonderiId))
            .OrderBy(y => y.OlusturmaZamani)
            .ToListAsync();

        var begeniler = await db.Begeniler
            .AsNoTracking()
            .Where(b => gonderiIdleri.Contains(b.GonderiId))
            .ToListAsync();

        // Tüm ilgili yazarların (gönderi + yorum) adlarını tek sorguda çöz.
        var yazarIdleri = gonderiler.Select(g => g.YazarId).Concat(yorumlar.Select(y => y.YazarId));
        var adlar = await YazarAdlariAsync(yazarIdleri);

        var yorumlarGrup = yorumlar.ToLookup(y => y.GonderiId);
        var begeniGrup = begeniler.ToLookup(b => b.GonderiId);

        return gonderiler.Select(g =>
        {
            var gonderiBegeni = begeniGrup[g.Id];
            var gonderiYorum = yorumlarGrup[g.Id]
                .Select(y => new YorumOzeti(
                    y,
                    adlar.GetValueOrDefault(y.YazarId, "?"),
                    Silebilir: yetki.IsAdmin || y.YazarId == yetki.KullaniciId))
                .ToList();

            return new GonderiOzeti(
                g,
                adlar.GetValueOrDefault(g.YazarId, "?"),
                gonderiBegeni.Count(),
                gonderiBegeni.Any(b => b.KullaniciId == yetki.KullaniciId),
                Silebilir: yetki.IsAdmin || g.YazarId == yetki.KullaniciId,
                gonderiYorum);
        }).ToList();
    }

    private async Task<Dictionary<string, string>> YazarAdlariAsync(IEnumerable<string> yazarIdleri)
    {
        var idler = yazarIdleri.Distinct().ToList();
        return await db.Users
            .AsNoTracking()
            .Where(u => idler.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.AdSoyad);
    }
}
