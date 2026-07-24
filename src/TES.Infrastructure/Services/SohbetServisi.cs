using Microsoft.EntityFrameworkCore;
using TES.Domain.Entities;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Services;

/// <summary>
/// Amir ↔ stajyer sohbeti. Konuşma izni (kimin kiminle konuşabileceği) her işlemde
/// SUNUCUDA doğrulanır (CLAUDE.md Bölüm 5, 10): stajyer yalnız amiriyle, amir yalnız
/// kendi stajyerleriyle; admin izler (gönderemez).
/// </summary>
public class SohbetServisi(TesDbContext db) : ISohbetServisi
{
    public async Task<bool> KonusabilirlerMiAsync(string kullaniciA, string kullaniciB)
    {
        if (string.IsNullOrEmpty(kullaniciA) || string.IsNullOrEmpty(kullaniciB) || kullaniciA == kullaniciB)
            return false;

        // A amir, B stajyer mi?
        var aAmir = await db.StajyerProfilleri
            .AnyAsync(s => s.KullaniciId == kullaniciB && s.Amir != null && s.Amir.KullaniciId == kullaniciA);
        if (aAmir)
            return true;

        // B amir, A stajyer mi?
        return await db.StajyerProfilleri
            .AnyAsync(s => s.KullaniciId == kullaniciA && s.Amir != null && s.Amir.KullaniciId == kullaniciB);
    }

    public async Task<IReadOnlyList<KonusmaKisi>> KonusmalarimAsync(YetkiBaglami yetki)
    {
        // Karşı taraf kullanıcı Id'leri (rol'e göre).
        List<string> karsiIdler;

        if (yetki.IsStajyer && !yetki.IsAmir && !yetki.IsAdmin)
        {
            karsiIdler = await db.StajyerProfilleri
                .Where(s => s.KullaniciId == yetki.KullaniciId && s.Amir != null)
                .Select(s => s.Amir!.KullaniciId)
                .ToListAsync();
        }
        else if (yetki.IsAmir)
        {
            karsiIdler = await db.StajyerProfilleri
                .Where(s => s.Amir != null && s.Amir.KullaniciId == yetki.KullaniciId)
                .Select(s => s.KullaniciId)
                .ToListAsync();
        }
        else
        {
            return []; // Admin bu listeyi kullanmaz (AdminKonusmalari'nı kullanır).
        }

        if (karsiIdler.Count == 0)
            return [];

        var adlar = await db.Users
            .Where(u => karsiIdler.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.AdSoyad);

        // Her kişiden gelen okunmamış mesaj sayısı.
        var okunmamis = await db.SohbetMesajlari
            .Where(m => m.AliciId == yetki.KullaniciId && !m.OkunduMu && karsiIdler.Contains(m.GondericiId))
            .GroupBy(m => m.GondericiId)
            .Select(g => new { g.Key, Adet = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Adet);

        return karsiIdler
            .Select(id => new KonusmaKisi(id, adlar.GetValueOrDefault(id, "?"), okunmamis.GetValueOrDefault(id, 0)))
            .OrderByDescending(k => k.OkunmamisSayisi)
            .ThenBy(k => k.AdSoyad)
            .ToList();
    }

    public async Task<IReadOnlyList<KonusmaCifti>> AdminKonusmalariAsync()
    {
        // Aralarında mesaj bulunan amir–stajyer çiftleri.
        var stajyerler = await db.StajyerProfilleri
            .Where(s => s.Amir != null)
            .Select(s => new { StajyerId = s.KullaniciId, AmirId = s.Amir!.KullaniciId })
            .ToListAsync();

        if (stajyerler.Count == 0)
            return [];

        var tumIdler = stajyerler.Select(s => s.StajyerId).Concat(stajyerler.Select(s => s.AmirId)).Distinct().ToList();
        var adlar = await db.Users
            .Where(u => tumIdler.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.AdSoyad);

        var sonuc = new List<KonusmaCifti>();
        foreach (var cift in stajyerler)
        {
            var mesajVar = await db.SohbetMesajlari.AnyAsync(m =>
                (m.GondericiId == cift.AmirId && m.AliciId == cift.StajyerId) ||
                (m.GondericiId == cift.StajyerId && m.AliciId == cift.AmirId));

            if (mesajVar)
                sonuc.Add(new KonusmaCifti(
                    cift.AmirId, adlar.GetValueOrDefault(cift.AmirId, "?"),
                    cift.StajyerId, adlar.GetValueOrDefault(cift.StajyerId, "?")));
        }

        return sonuc;
    }

    public async Task<(bool Yetkili, IReadOnlyList<SohbetMesaji> Mesajlar)> MesajlariGetirAsync(
        string benId, string karsiId, YetkiBaglami yetki, bool okunduIsaretle)
    {
        // Admin herhangi iki tarafı izleyebilir; diğerleri yalnız kendi konuşmalarını.
        var yetkili = yetki.IsAdmin
            ? true
            : (benId == yetki.KullaniciId && await KonusabilirlerMiAsync(benId, karsiId));

        if (!yetkili)
            return (false, []);

        var mesajlar = await db.SohbetMesajlari
            .Where(m => (m.GondericiId == benId && m.AliciId == karsiId) ||
                        (m.GondericiId == karsiId && m.AliciId == benId))
            .OrderBy(m => m.Zaman)
            .ToListAsync();

        // Karşıdan gelen okunmamışları okundu işaretle (yalnız kendi gelen kutusu; admin izlerken değil).
        if (okunduIsaretle && !yetki.IsAdmin)
        {
            var okunacak = mesajlar.Where(m => m.AliciId == benId && !m.OkunduMu).ToList();
            if (okunacak.Count > 0)
            {
                foreach (var m in okunacak)
                    m.OkunduMu = true;
                await db.SaveChangesAsync();
            }
        }

        return (true, mesajlar);
    }

    public async Task<MesajSonucu> MesajGonderAsync(string gondericiId, string aliciId, string icerik)
    {
        if (string.IsNullOrWhiteSpace(icerik))
            return new MesajSonucu(false, "Mesaj boş olamaz.", null);

        if (!await KonusabilirlerMiAsync(gondericiId, aliciId))
            return new MesajSonucu(false, "Bu kişiyle mesajlaşma yetkiniz yok.", null);

        var mesaj = new SohbetMesaji
        {
            GondericiId = gondericiId,
            AliciId = aliciId,
            Icerik = icerik.Trim(),
            Zaman = DateTime.UtcNow
        };

        db.SohbetMesajlari.Add(mesaj);
        await db.SaveChangesAsync();

        return new MesajSonucu(true, null, mesaj);
    }

    public Task<int> OkunmamisToplamAsync(string kullaniciId) =>
        db.SohbetMesajlari.CountAsync(m => m.AliciId == kullaniciId && !m.OkunduMu);

    public async Task<bool> OkunduIsaretleAsync(string benId, string karsiId)
    {
        var okunacak = await db.SohbetMesajlari
            .Where(m => m.AliciId == benId && m.GondericiId == karsiId && !m.OkunduMu)
            .ToListAsync();

        if (okunacak.Count == 0)
            return false;

        foreach (var m in okunacak)
            m.OkunduMu = true;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<string> AdSoyadAsync(string kullaniciId) =>
        (await db.Users.Where(u => u.Id == kullaniciId).Select(u => u.AdSoyad).FirstOrDefaultAsync()) ?? "?";
}
