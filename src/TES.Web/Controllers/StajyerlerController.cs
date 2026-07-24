using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TES.Domain.Sabitler;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>
/// Stajyer listesi/detayı. Admin tümünü, Amir yalnızca KENDİ stajyerlerini görür;
/// sahiplik sunucuda IStajyerSorguServisi ile doğrulanır (CLAUDE.md Bölüm 5).
/// </summary>
[Authorize(Policy = Politikalar.AmirPolicy)]
public class StajyerlerController(
    IStajyerSorguServisi stajyerSorgu,
    IYoklamaServisi yoklamaServisi,
    IProjeServisi projeServisi,
    IOdevServisi odevServisi,
    IDenetimServisi denetim) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var stajyerler = User.IsInRole(Roller.Admin)
            ? await stajyerSorgu.TumunuListeleAsync()
            : await stajyerSorgu.AmirinStajyerleriAsync(AktifKullaniciId());

        return View(stajyerler);
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int id)
    {
        var satir = await stajyerSorgu.DetayGetirAsync(
            id,
            AktifKullaniciId(),
            isteyenAdmin: User.IsInRole(Roller.Admin),
            isteyenAmir: User.IsInRole(Roller.Amir));

        if (satir is null)
            return NotFound(); // yetkisiz erişimde de kaynak var/yok bilgisi sızdırılmaz

        // Hassas veri görüntüleme denetim kaydına yazılır (CLAUDE.md Bölüm 3).
        await denetim.KaydetAsync(User.Identity?.Name ?? "Bilinmiyor",
            "StajyerVerisiGoruntulendi", $"StajyerProfil: {id}");

        var yoklamalar = await yoklamaServisi.KayitlariGetirAsync([satir.Profil.Id]);

        ViewBag.Yoklamalar = yoklamalar
            .Select(y => new YoklamaSatirViewModel
            {
                StajyerAdSoyad = satir.AdSoyad,
                GirisZamani = y.GirisZamani,
                CikisZamani = y.CikisZamani
            })
            .ToList();

        // Faz 3: proje + ödevler (amir/admin bu stajyeri yönetebilir).
        ViewBag.Proje = await projeServisi.StajyerinProjesiAsync(satir.Profil.Id);
        ViewBag.Odevler = await odevServisi.StajyerinOdevleriAsync(satir.Profil.Id);

        return View(satir);
    }

    private string AktifKullaniciId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
}
