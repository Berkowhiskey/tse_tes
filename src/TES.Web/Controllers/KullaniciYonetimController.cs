using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>
/// Amir/Stajyer hesabı açma ve Amir–Stajyer eşleştirme — yalnızca Admin (CLAUDE.md Bölüm 5).
/// T.C. Kimlik No yalnızca geçici parola olarak Identity'ye iletilir; saklanmaz, loglanmaz.
/// </summary>
[Authorize(Policy = Politikalar.AdminPolicy)]
public class KullaniciYonetimController(
    IKullaniciYonetimServisi kullaniciYonetim,
    IStajyerSorguServisi stajyerSorgu,
    IDepartmanServisi departmanServisi) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new KullaniciYonetimIndexViewModel
        {
            Amirler = await kullaniciYonetim.AmirleriListeleAsync(),
            Stajyerler = await stajyerSorgu.TumunuListeleAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> AmirOlustur()
    {
        return View(new AmirOlusturViewModel { DepartmanSecenekleri = await DepartmanSecenekleriAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AmirOlustur(AmirOlusturViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.DepartmanSecenekleri = await DepartmanSecenekleriAsync();
            return View(model);
        }

        var sonuc = await kullaniciYonetim.AmirOlusturAsync(
            new YeniAmirBilgisi(model.Ad, model.Soyad, model.TcKimlikNo, model.DepartmanId!.Value,
                model.IseBaslamaTarihi, model.Hakkimda),
            AktifKullaniciAdi());

        if (!sonuc.Basarili)
        {
            foreach (var hata in sonuc.Hatalar)
                ModelState.AddModelError(string.Empty, hata);

            model.DepartmanSecenekleri = await DepartmanSecenekleriAsync();
            return View(model);
        }

        TempData["Basari"] = $"Amir oluşturuldu. Kullanıcı adı: {sonuc.KullaniciAdi} — ilk girişte parola değişimi zorunludur.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> StajyerOlustur()
    {
        return View(new StajyerOlusturViewModel { AmirSecenekleri = await AmirSecenekleriAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StajyerOlustur(StajyerOlusturViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AmirSecenekleri = await AmirSecenekleriAsync();
            return View(model);
        }

        var sonuc = await kullaniciYonetim.StajyerOlusturAsync(
            new YeniStajyerBilgisi(model.Ad, model.Soyad, model.TcKimlikNo, model.KartNo,
                model.StajBaslangic!.Value, model.StajBitis!.Value, model.Okul, model.Bolum, model.AmirProfilId),
            AktifKullaniciAdi());

        if (!sonuc.Basarili)
        {
            foreach (var hata in sonuc.Hatalar)
                ModelState.AddModelError(string.Empty, hata);

            model.AmirSecenekleri = await AmirSecenekleriAsync();
            return View(model);
        }

        TempData["Basari"] = $"Stajyer oluşturuldu. Kullanıcı adı: {sonuc.KullaniciAdi} — ilk girişte parola değişimi zorunludur.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Eslestir(int id)
    {
        var stajyer = await stajyerSorgu.DetayGetirAsync(id, AktifKullaniciId(), isteyenAdmin: true, isteyenAmir: false);
        if (stajyer is null)
            return NotFound();

        return View(new EslestirViewModel
        {
            StajyerProfilId = id,
            StajyerAdSoyad = stajyer.AdSoyad,
            AmirProfilId = stajyer.Profil.AmirId,
            AmirSecenekleri = await AmirSecenekleriAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eslestir(EslestirViewModel model)
    {
        var (basarili, hata) = await kullaniciYonetim.StajyerAmireAtaAsync(
            model.StajyerProfilId, model.AmirProfilId, AktifKullaniciAdi());

        if (!basarili)
        {
            ModelState.AddModelError(string.Empty, hata!);
            model.AmirSecenekleri = await AmirSecenekleriAsync();
            return View(model);
        }

        TempData["Basari"] = "Eşleştirme güncellendi (departman amirden alındı).";
        return RedirectToAction(nameof(Index));
    }

    private string AktifKullaniciAdi() => User.Identity?.Name ?? "Bilinmiyor";

    private string AktifKullaniciId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    private async Task<IEnumerable<SelectListItem>> DepartmanSecenekleriAsync()
    {
        var liste = await departmanServisi.HiyerarsikListeAsync();
        return liste.Select(x => new SelectListItem(
            $"{new string('—', x.Seviye)} {x.Departman.Ad}".Trim(),
            x.Departman.Id.ToString()));
    }

    private async Task<IEnumerable<SelectListItem>> AmirSecenekleriAsync()
    {
        var amirler = await kullaniciYonetim.AmirleriListeleAsync();
        return amirler.Select(a => new SelectListItem($"{a.AdSoyad} ({a.DepartmanAd})", a.ProfilId.ToString()));
    }
}
