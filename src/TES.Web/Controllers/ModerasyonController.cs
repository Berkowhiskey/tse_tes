using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>
/// Gönderi moderasyonu — yalnızca Admin (CLAUDE.md Bölüm 5). Bekleyen stajyer gönderilerini
/// onaylar veya gerekçeli reddeder. Amir yardımcı olabilir ama moderasyon yetkisi yoktur.
/// </summary>
[Authorize(Policy = Politikalar.AdminPolicy)]
public class ModerasyonController(ISosyalServis sosyalServis) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await sosyalServis.ModerasyonBekleyenlerAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onayla(int gonderiId)
    {
        var sonuc = await sosyalServis.OnaylaAsync(gonderiId, User.Identity?.Name ?? "Bilinmiyor");
        TempData[sonuc.Basarili ? "Basari" : "Hata"] = sonuc.Basarili ? "Gönderi onaylandı ve yayınlandı." : sonuc.Hata;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Reddet(int gonderiId)
    {
        var bekleyenler = await sosyalServis.ModerasyonBekleyenlerAsync();
        var ozet = bekleyenler.FirstOrDefault(o => o.Gonderi.Id == gonderiId);
        if (ozet is null)
            return RedirectToAction(nameof(Index));

        return View(new ReddetViewModel
        {
            GonderiId = gonderiId,
            YazarAdSoyad = ozet.YazarAdSoyad,
            Icerik = ozet.Gonderi.Icerik
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reddet(ReddetViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var sonuc = await sosyalServis.ReddetAsync(model.GonderiId, model.RedMesaji, User.Identity?.Name ?? "Bilinmiyor");

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View(model);
        }

        TempData["Basari"] = "Gönderi reddedildi; gerekçe yazara iletilecek.";
        return RedirectToAction(nameof(Index));
    }
}
