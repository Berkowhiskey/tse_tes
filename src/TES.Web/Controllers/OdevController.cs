using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TES.Domain.Entities;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>
/// Ödev takibi. Amir kendi stajyerine ödev atar; Stajyer kendi ödevlerini görür ve ilerleme
/// raporlar; Admin hepsi. Sahiplik OdevServisi'nde sunucuda doğrulanır.
/// </summary>
[Authorize(Policy = Politikalar.StajyerPolicy)]
public class OdevController(
    IOdevServisi odevServisi,
    IStajyerSorguServisi stajyerSorgu) : Controller
{
    /// <summary>Stajyerin kendi ödevleri + durum güncelleme.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var odevler = await odevServisi.KendiOdevlerimAsync(AktifKullaniciId());
        return View(odevler);
    }

    /// <summary>Amirin/Adminin verdiği ödevler.</summary>
    [HttpGet]
    [Authorize(Policy = Politikalar.AmirPolicy)]
    public async Task<IActionResult> Verdiklerim()
    {
        var odevler = await odevServisi.AmirinAttiklariAsync(AktifKullaniciId());

        // Stajyer ad-soyadlarını (Kullanici tablosunda) tek seferde çözüp eşleştir.
        var adlar = (await stajyerSorgu.AmirinStajyerleriAsync(AktifKullaniciId()))
            .ToDictionary(s => s.Profil.Id, s => s.AdSoyad);

        var satirlar = odevler
            .Select(o => new OdevSatiri(o, adlar.GetValueOrDefault(o.StajyerProfilId)))
            .ToList();

        return View(satirlar);
    }

    [HttpGet]
    [Authorize(Policy = Politikalar.AmirPolicy)]
    public async Task<IActionResult> Ata(int stajyerId)
    {
        var stajyer = await stajyerSorgu.DetayGetirAsync(stajyerId, AktifKullaniciId(),
            User.IsInRole(Domain.Sabitler.Roller.Admin), User.IsInRole(Domain.Sabitler.Roller.Amir));
        if (stajyer is null)
            return NotFound();

        return View(new OdevAtaViewModel { StajyerProfilId = stajyerId, StajyerAdSoyad = stajyer.AdSoyad });
    }

    [HttpPost]
    [Authorize(Policy = Politikalar.AmirPolicy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ata(OdevAtaViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var sonuc = await odevServisi.AtaAsync(
            model.StajyerProfilId, model.Baslik, model.Aciklama, model.TeslimTarihi, User.YetkiBaglamiOlustur());

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View(model);
        }

        TempData["Basari"] = "Ödev atandı.";
        return RedirectToAction("Detay", "Stajyerler", new { id = model.StajyerProfilId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumGuncelle(int odevId, int ilerleme, IsDurumu durum, string? donusUrl)
    {
        var sonuc = await odevServisi.DurumGuncelleAsync(odevId, ilerleme, durum, User.YetkiBaglamiOlustur());

        if (sonuc.Basarili)
            TempData["Basari"] = "Ödev durumu güncellendi.";
        else
            TempData["Hata"] = sonuc.Hata;

        return Donus(donusUrl);
    }

    [HttpPost]
    [Authorize(Policy = Politikalar.AmirPolicy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int odevId, int stajyerId)
    {
        var sonuc = await odevServisi.SilAsync(odevId, User.YetkiBaglamiOlustur());

        if (sonuc.Basarili)
            TempData["Basari"] = "Ödev silindi.";
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
