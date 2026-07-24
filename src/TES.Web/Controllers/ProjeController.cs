using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TES.Domain.Entities;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>
/// Proje takibi. Stajyer kendi projesini görür ve ilerleme raporlar; Amir kendi stajyerine
/// proje atar/yönetir; Admin hepsi. Sahiplik ProjeServisi'nde sunucuda doğrulanır.
/// </summary>
[Authorize(Policy = Politikalar.StajyerPolicy)]
public class ProjeController(
    IProjeServisi projeServisi,
    IStajyerSorguServisi stajyerSorgu) : Controller
{
    /// <summary>Stajyerin kendi projesi + ilerleme güncelleme.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var proje = await projeServisi.KendiProjemAsync(AktifKullaniciId());
        return View(new ProjemViewModel { Proje = proje });
    }

    [HttpGet]
    [Authorize(Policy = Politikalar.AmirPolicy)]
    public async Task<IActionResult> Yonet(int stajyerId)
    {
        var stajyer = await stajyerSorgu.DetayGetirAsync(stajyerId, AktifKullaniciId(),
            User.IsInRole(Domain.Sabitler.Roller.Admin), User.IsInRole(Domain.Sabitler.Roller.Amir));
        if (stajyer is null)
            return NotFound();

        var mevcut = await projeServisi.StajyerinProjesiAsync(stajyerId);

        return View(new ProjeYonetViewModel
        {
            StajyerProfilId = stajyerId,
            StajyerAdSoyad = stajyer.AdSoyad,
            Duzenleme = mevcut is not null,
            Ad = mevcut?.Ad ?? string.Empty,
            Aciklama = mevcut?.Aciklama
        });
    }

    [HttpPost]
    [Authorize(Policy = Politikalar.AmirPolicy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yonet(ProjeYonetViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var sonuc = await projeServisi.KaydetAsync(model.StajyerProfilId, model.Ad, model.Aciklama, User.YetkiBaglamiOlustur());

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View(model);
        }

        TempData["Basari"] = "Proje kaydedildi.";
        return RedirectToAction("Detay", "Stajyerler", new { id = model.StajyerProfilId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IlerlemeGuncelle(int projeId, int ilerleme, IsDurumu durum, string? donusUrl)
    {
        var sonuc = await projeServisi.IlerlemeGuncelleAsync(projeId, ilerleme, durum, User.YetkiBaglamiOlustur());

        if (sonuc.Basarili)
            TempData["Basari"] = "Proje ilerlemesi güncellendi.";
        else
            TempData["Hata"] = sonuc.Hata;

        return Donus(donusUrl);
    }

    [HttpPost]
    [Authorize(Policy = Politikalar.AmirPolicy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int projeId, int stajyerId)
    {
        var sonuc = await projeServisi.SilAsync(projeId, User.YetkiBaglamiOlustur());

        if (sonuc.Basarili)
            TempData["Basari"] = "Proje silindi.";
        else
            TempData["Hata"] = sonuc.Hata;

        return RedirectToAction("Detay", "Stajyerler", new { id = stajyerId });
    }

    private string AktifKullaniciId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    private IActionResult Donus(string? donusUrl) =>
        !string.IsNullOrEmpty(donusUrl) && Url.IsLocalUrl(donusUrl)
            ? Redirect(donusUrl)
            : RedirectToAction(nameof(Index));
}
