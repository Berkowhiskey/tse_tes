using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TES.Domain.Sabitler;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>
/// Sosyal akış: gönderi oluşturma, yorum, beğeni ve içerik kaldırma.
/// Stajyer gönderisi onaya gider; sahiplik/rol denetimi SosyalServis'te sunucuda yapılır.
/// </summary>
[Authorize(Policy = Politikalar.StajyerPolicy)]
public class GonderiController(ISosyalServis sosyalServis) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var yetki = User.YetkiBaglamiOlustur();

        return View(new FeedViewModel
        {
            Gonderiler = await sosyalServis.FeedGetirAsync(yetki),
            OnayaTabi = User.IsInRole(Roller.Stajyer) && !User.IsInRole(Roller.Amir) && !User.IsInRole(Roller.Admin)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Olustur(FeedViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Gonderiler = await sosyalServis.FeedGetirAsync(User.YetkiBaglamiOlustur());
            return View(nameof(Index), model);
        }

        var durum = await sosyalServis.GonderiOlusturAsync(model.YeniIcerik, User.YetkiBaglamiOlustur());

        TempData["Basari"] = durum == Domain.Entities.ModerasyonDurumu.Beklemede
            ? "Gönderiniz admin onayına gönderildi. Onaylanınca yayında görünecek."
            : "Gönderiniz yayınlandı.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int gonderiId)
    {
        var sonuc = await sosyalServis.GonderiSilAsync(gonderiId, User.YetkiBaglamiOlustur());
        TempData[sonuc.Basarili ? "Basari" : "Hata"] = sonuc.Basarili ? "Gönderi kaldırıldı." : sonuc.Hata;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YorumEkle(int gonderiId, string icerik)
    {
        var sonuc = await sosyalServis.YorumEkleAsync(gonderiId, icerik ?? string.Empty, User.YetkiBaglamiOlustur());
        if (!sonuc.Basarili)
            TempData["Hata"] = sonuc.Hata;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YorumSil(int yorumId)
    {
        var sonuc = await sosyalServis.YorumSilAsync(yorumId, User.YetkiBaglamiOlustur());
        TempData[sonuc.Basarili ? "Basari" : "Hata"] = sonuc.Basarili ? "Yorum kaldırıldı." : sonuc.Hata;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Begen(int gonderiId)
    {
        var (basarili, _, hata) = await sosyalServis.BegeniToggleAsync(gonderiId, User.YetkiBaglamiOlustur());
        if (!basarili)
            TempData["Hata"] = hata;

        return RedirectToAction(nameof(Index));
    }
}
