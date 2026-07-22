using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TES.Domain.Sabitler;
using TES.Infrastructure.Identity;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>Kullanıcının KENDİ profili — sahiplik oturumdaki kimlikten gelir.</summary>
[Authorize]
public class ProfilController(
    UserManager<Kullanici> userManager,
    IStajyerSorguServisi stajyerSorgu,
    IProfilServisi profilServisi) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var kullanici = await userManager.GetUserAsync(User);
        if (kullanici is null)
            return RedirectToAction("Giris", "Hesap");

        var roller = await userManager.GetRolesAsync(kullanici);

        var model = new ProfilViewModel
        {
            AdSoyad = kullanici.AdSoyad,
            KullaniciAdi = kullanici.UserName ?? "?",
            Rol = string.Join(", ", roller)
        };

        if (roller.Contains(Roller.Stajyer))
        {
            model.StajyerBilgisi = await stajyerSorgu.KendiProfilimAsync(kullanici.Id);
        }
        else if (roller.Contains(Roller.Amir))
        {
            var amirBilgi = await profilServisi.AmirProfilGetirAsync(kullanici.Id);
            model.DepartmanAd = amirBilgi?.DepartmanAd;
            model.IseBaslamaTarihi = amirBilgi?.IseBaslamaTarihi;
            model.Hakkimda = amirBilgi?.Hakkimda;
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Duzenle()
    {
        var kullanici = await userManager.GetUserAsync(User);
        if (kullanici is null)
            return RedirectToAction("Giris", "Hesap");

        var roller = await userManager.GetRolesAsync(kullanici);
        var model = new ProfilDuzenleViewModel { StajyerMi = roller.Contains(Roller.Stajyer) };

        if (model.StajyerMi)
        {
            var satir = await stajyerSorgu.KendiProfilimAsync(kullanici.Id);
            if (satir is null)
                return NotFound();

            model.Okul = satir.Profil.Okul;
            model.Bolum = satir.Profil.Bolum;
        }
        else if (roller.Contains(Roller.Amir))
        {
            var amirBilgi = await profilServisi.AmirProfilGetirAsync(kullanici.Id);
            if (amirBilgi is null)
                return NotFound();

            model.Hakkimda = amirBilgi.Hakkimda;
        }
        else
        {
            // Admin'in düzenlenecek alan profili yok (CLAUDE.md Bölüm 5: Profil düzenleme "—").
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(ProfilDuzenleViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var kullanici = await userManager.GetUserAsync(User);
        if (kullanici is null)
            return RedirectToAction("Giris", "Hesap");

        var roller = await userManager.GetRolesAsync(kullanici);
        var aktor = kullanici.UserName ?? "Bilinmiyor";

        // Rol sunucuda doğrulanır; formdaki StajyerMi değerine güvenilmez.
        var basarili = roller.Contains(Roller.Stajyer)
            ? await profilServisi.StajyerBilgiGuncelleAsync(kullanici.Id, model.Okul, model.Bolum, aktor)
            : roller.Contains(Roller.Amir) &&
              await profilServisi.AmirHakkimdaGuncelleAsync(kullanici.Id, model.Hakkimda, aktor);

        if (!basarili)
            return NotFound();

        TempData["Basari"] = "Profiliniz güncellendi.";
        return RedirectToAction(nameof(Index));
    }
}
