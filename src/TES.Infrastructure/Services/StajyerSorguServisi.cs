using Microsoft.EntityFrameworkCore;
using TES.Infrastructure.Data;

namespace TES.Infrastructure.Services;

public class StajyerSorguServisi(TesDbContext db) : IStajyerSorguServisi
{
    public Task<IReadOnlyList<StajyerSatiri>> TumunuListeleAsync() =>
        SorgulaAsync(null);

    public Task<IReadOnlyList<StajyerSatiri>> AmirinStajyerleriAsync(string amirKullaniciId) =>
        SorgulaAsync(s => s.Amir != null && s.Amir.KullaniciId == amirKullaniciId);

    public async Task<StajyerSatiri?> DetayGetirAsync(int stajyerProfilId, string isteyenKullaniciId, bool isteyenAdmin, bool isteyenAmir)
    {
        var satirlar = await SorgulaAsync(s => s.Id == stajyerProfilId);
        var satir = satirlar.FirstOrDefault();
        if (satir is null)
            return null;

        // Sahiplik kuralı sunucuda: admin her stajyeri, amir kendi stajyerini, stajyer kendini.
        if (isteyenAdmin)
            return satir;

        if (isteyenAmir && satir.Profil.Amir?.KullaniciId == isteyenKullaniciId)
            return satir;

        if (satir.Profil.KullaniciId == isteyenKullaniciId)
            return satir;

        return null;
    }

    public async Task<StajyerSatiri?> KendiProfilimAsync(string kullaniciId)
    {
        var satirlar = await SorgulaAsync(s => s.KullaniciId == kullaniciId);
        return satirlar.FirstOrDefault();
    }

    private async Task<IReadOnlyList<StajyerSatiri>> SorgulaAsync(
        System.Linq.Expressions.Expression<Func<Domain.Entities.StajyerProfil, bool>>? filtre)
    {
        var sorgu = db.StajyerProfilleri
            .AsNoTracking()
            .Include(s => s.Amir)
            .Include(s => s.Departman)
            .AsQueryable();

        if (filtre is not null)
            sorgu = sorgu.Where(filtre);

        // Kullanıcı adları Identity tablosundan projection ile alınır.
        var veri = await sorgu
            .Select(s => new
            {
                Profil = s,
                Kullanici = db.Users.First(u => u.Id == s.KullaniciId),
                AmirKullanici = s.Amir == null ? null : db.Users.FirstOrDefault(u => u.Id == s.Amir.KullaniciId)
            })
            .ToListAsync();

        return veri
            .Select(x => new StajyerSatiri(
                x.Profil,
                x.Kullanici.UserName ?? "?",
                x.Kullanici.AdSoyad,
                x.AmirKullanici?.AdSoyad,
                x.Profil.Departman?.Ad))
            .OrderBy(x => x.AdSoyad)
            .ToList();
    }
}
