using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TES.Infrastructure.Identity;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>
/// Giriş / çıkış / zorunlu parola değişimi. Girişler ve parola değişimleri
/// DenetimKaydi'na yazılır; parola hiçbir yerde loglanmaz.
/// </summary>
public class HesapController(
    SignInManager<Kullanici> signInManager,
    UserManager<Kullanici> userManager,
    IDenetimServisi denetim) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Giris()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(GirisViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var sonuc = await signInManager.PasswordSignInAsync(
            model.KullaniciAdi, model.Parola, model.BeniHatirla, lockoutOnFailure: true);

        if (sonuc.Succeeded)
        {
            await denetim.KaydetAsync(model.KullaniciAdi, "Giris");

            var kullanici = await userManager.FindByNameAsync(model.KullaniciAdi);
            if (kullanici?.ForceChangePassword == true)
                return RedirectToAction(nameof(ParolaDegistir));

            return RedirectToAction("Index", "Home");
        }

        if (sonuc.IsLockedOut)
        {
            await denetim.KaydetAsync(model.KullaniciAdi, "GirisKilitlendi",
                "Üst üste başarısız denemeler nedeniyle hesap geçici kilitlendi.");
            ModelState.AddModelError(string.Empty,
                "Hesabınız geçici olarak kilitlendi. Lütfen birkaç dakika sonra tekrar deneyin.");
            return View(model);
        }

        await denetim.KaydetAsync(model.KullaniciAdi, "GirisBasarisiz");
        ModelState.AddModelError(string.Empty, "Kullanıcı adı veya parola hatalı.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cikis()
    {
        var kullaniciAdi = User.Identity?.Name ?? "Bilinmiyor";
        await signInManager.SignOutAsync();
        await denetim.KaydetAsync(kullaniciAdi, "Cikis");
        return RedirectToAction(nameof(Giris));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ParolaDegistir()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ParolaDegistir(ParolaDegistirViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var kullanici = await userManager.GetUserAsync(User);
        if (kullanici is null)
            return RedirectToAction(nameof(Giris));

        var sonuc = await userManager.ChangePasswordAsync(kullanici, model.MevcutParola, model.YeniParola);

        if (!sonuc.Succeeded)
        {
            foreach (var hata in sonuc.Errors)
                ModelState.AddModelError(string.Empty, HataMesajiCevir(hata));

            return View(model);
        }

        // Zorunlu değişim tamamlandı; bayrağı kapat ve oturumu tazele.
        if (kullanici.ForceChangePassword)
        {
            kullanici.ForceChangePassword = false;
            await userManager.UpdateAsync(kullanici);
        }

        await signInManager.RefreshSignInAsync(kullanici);
        await denetim.KaydetAsync(kullanici.UserName ?? "Bilinmiyor", "ParolaDegisti");

        TempData["Basari"] = "Parolanız başarıyla değiştirildi.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ErisimReddedildi()
    {
        return View();
    }

    /// <summary>Identity'nin İngilizce hata mesajlarını kullanıcıya Türkçe gösterir.</summary>
    private static string HataMesajiCevir(IdentityError hata) => hata.Code switch
    {
        "PasswordMismatch" => "Mevcut parola hatalı.",
        "PasswordTooShort" => "Yeni parola çok kısa (en az 8 karakter).",
        "PasswordRequiresDigit" => "Yeni parola en az bir rakam içermelidir.",
        _ => hata.Description
    };
}
