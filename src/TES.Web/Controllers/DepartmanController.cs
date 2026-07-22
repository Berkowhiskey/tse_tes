using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>Departman hiyerarşisi yönetimi — yalnızca Admin (CLAUDE.md Bölüm 5).</summary>
[Authorize(Policy = Politikalar.AdminPolicy)]
public class DepartmanController(IDepartmanServisi departmanServisi) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var liste = await departmanServisi.HiyerarsikListeAsync();

        return View(new DepartmanListeViewModel
        {
            Satirlar = liste
                .Select(x => new DepartmanSatirViewModel { Id = x.Departman.Id, Ad = x.Departman.Ad, Seviye = x.Seviye })
                .ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Olustur()
    {
        return View("Form", new DepartmanFormViewModel
        {
            DepartmanSecenekleri = await SecenekleriGetirAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Duzenle(int id)
    {
        var departman = await departmanServisi.GetirAsync(id);
        if (departman is null)
            return NotFound();

        return View("Form", new DepartmanFormViewModel
        {
            Id = departman.Id,
            Ad = departman.Ad,
            UstDepartmanId = departman.UstDepartmanId,
            DepartmanSecenekleri = await SecenekleriGetirAsync(haricId: id)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Kaydet(DepartmanFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.DepartmanSecenekleri = await SecenekleriGetirAsync(haricId: model.Id);
            return View("Form", model);
        }

        try
        {
            if (model.Id is null)
                await departmanServisi.OlusturAsync(model.Ad, model.UstDepartmanId, AktifKullaniciAdi());
            else
                await departmanServisi.GuncelleAsync(model.Id.Value, model.Ad, model.UstDepartmanId, AktifKullaniciAdi());
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.DepartmanSecenekleri = await SecenekleriGetirAsync(haricId: model.Id);
            return View("Form", model);
        }

        TempData["Basari"] = "Departman kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var (basarili, hata) = await departmanServisi.SilAsync(id, AktifKullaniciAdi());

        if (basarili)
            TempData["Basari"] = "Departman silindi.";
        else
            TempData["Hata"] = hata;

        return RedirectToAction(nameof(Index));
    }

    private string AktifKullaniciAdi() => User.Identity?.Name ?? "Bilinmiyor";

    private async Task<IEnumerable<SelectListItem>> SecenekleriGetirAsync(int? haricId = null)
    {
        var liste = await departmanServisi.HiyerarsikListeAsync();

        return liste
            .Where(x => x.Departman.Id != haricId)
            .Select(x => new SelectListItem(
                $"{new string('—', x.Seviye)} {x.Departman.Ad}".Trim(),
                x.Departman.Id.ToString()));
    }
}
