using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TES.Domain.Sabitler;
using TES.Infrastructure.Identity;
using TES.Infrastructure.Services;
using TES.Infrastructure.Simulation;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

/// <summary>
/// TSE-Misafir akışının iki giriş kapısı (CLAUDE.md Bölüm 8):
/// (a) anonim portal — henüz ağa girmemiş kişi, (b) stajyerin "Taleplerim" sayfası.
/// Sponsor onayı, durum sayfası (30 sn yenileme) ve voucher ile cihaz ekleme de buradadır.
/// </summary>
public class MisafirController(
    IMisafirTalepServisi talepServisi,
    INetworkAccessProvider agSaglayici,
    IStajyerSorguServisi stajyerSorgu,
    UserManager<Kullanici> userManager) : Controller
{
    // ---- (a) Anonim portal ----

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Portal()
    {
        return View(new MisafirTalepFormViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Portal(MisafirTalepFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var sonuc = await talepServisi.TalepOlusturAsync(
            new YeniMisafirTalebi(model.AdSoyad, model.Eposta, model.SponsorEposta, model.SureGun, StajyerProfilId: null),
            OnayLinkSablonu());

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View(model);
        }

        return RedirectToAction(nameof(Durum), new { kod = sonuc.TakipKodu });
    }

    // ---- Durum sayfası (30 sn'de bir kendini yeniler) ----

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Durum(Guid kod)
    {
        var talep = await talepServisi.TakipKoduIleGetirAsync(kod);
        if (talep is null)
            return NotFound();

        var agDurumu = await agSaglayici.DurumGetirAsync(talep.Id);
        return View(new MisafirDurumViewModel { Talep = talep, AgDurumu = agDurumu });
    }

    // ---- Sponsor onay ekranı (e-postadaki tek kullanımlık bağlantı) ----

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Onay(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return NotFound();

        var talep = await talepServisi.TokenIleGetirAsync(token);
        if (talep is null)
            return View("OnayGecersiz"); // geçersiz/kullanılmış/süresi dolmuş bağlantı

        return View(new MisafirOnayViewModel { Token = token, Talep = talep });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnayKarar(string token, bool onay)
    {
        var (basarili, hata) = await talepServisi.KararVerAsync(token, onay);

        if (!basarili)
        {
            TempData["OnaySonucHata"] = hata;
            return View("OnayGecersiz");
        }

        ViewBag.Onaylandi = onay;
        return View("OnaySonuc");
    }

    // ---- Voucher ile cihaz ekleme ----

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Voucher()
    {
        return View(new VoucherViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Voucher(VoucherViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (basarili, mesaj) = await talepServisi.VoucherKullanAsync(model.Voucher);
        model.Basarili = basarili;
        model.Mesaj = mesaj;
        return View(model);
    }

    // ---- (b) Stajyerin talepleri ----

    [HttpGet]
    [Authorize(Roles = Roller.Stajyer)]
    public async Task<IActionResult> Taleplerim()
    {
        var kullanici = await userManager.GetUserAsync(User);
        if (kullanici is null)
            return RedirectToAction("Giris", "Hesap");

        return View(new TaleplerimViewModel
        {
            Form = new StajyerTalepFormViewModel(),
            Talepler = await talepServisi.StajyerinTalepleriAsync(kullanici.Id)
        });
    }

    [HttpPost]
    [Authorize(Roles = Roller.Stajyer)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TalepEt(StajyerTalepFormViewModel form)
    {
        var kullanici = await userManager.GetUserAsync(User);
        if (kullanici is null)
            return RedirectToAction("Giris", "Hesap");

        // Ad Soyad ve stajyer bağı SUNUCUDA kimlikten alınır; formdan gelene güvenilmez.
        var profil = await stajyerSorgu.KendiProfilimAsync(kullanici.Id);

        if (!ModelState.IsValid)
        {
            return View("Taleplerim", new TaleplerimViewModel
            {
                Form = form,
                Talepler = await talepServisi.StajyerinTalepleriAsync(kullanici.Id)
            });
        }

        var sonuc = await talepServisi.TalepOlusturAsync(
            new YeniMisafirTalebi(kullanici.AdSoyad, form.Eposta, form.SponsorEposta, form.SureGun, profil?.Profil.Id),
            OnayLinkSablonu());

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Hata!);
            return View("Taleplerim", new TaleplerimViewModel
            {
                Form = form,
                Talepler = await talepServisi.StajyerinTalepleriAsync(kullanici.Id)
            });
        }

        TempData["Basari"] = "Talebiniz oluşturuldu; sponsor onayı bekleniyor.";
        return RedirectToAction(nameof(Durum), new { kod = sonuc.TakipKodu });
    }

    /// <summary>E-postaya yazılacak onay bağlantısı şablonu; "{0}" yerine ham token gelir.</summary>
    private string OnayLinkSablonu() =>
        $"{Url.Action(nameof(Onay), "Misafir", null, Request.Scheme)}?token={{0}}";
}
