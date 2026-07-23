using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TES.Domain.Sabitler;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

// GERÇEKTE: Bu ekranın yerini kurum girişindeki fiziksel RFID gişeleri alır; gişe cihazı
// kart okuttuğunda aynı IYoklamaServisi.KartOkutAsync akışı tetiklenir. Simülatör yalnızca
// geliştirme/demoda kart okutma olayını üretir (CLAUDE.md Bölüm 9).
/// <summary>
/// RFID yoklama simülatörü. Admin tüm kartları okutabilir; Stajyer YALNIZCA kendi kartını
/// okutabilir — kart seçimi sunucuda kimlikten çözülür, formdan gelen değere güvenilmez.
/// </summary>
[Authorize(Policy = Politikalar.StajyerPolicy)]
public class YoklamaSimulatorController(
    IStajyerSorguServisi stajyerSorgu,
    IYoklamaServisi yoklamaServisi) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await ModelHazirlaAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KartOkut(string? kartNo)
    {
        var model = await ModelHazirlaAsync();

        // Okutulacak kart SUNUCUDA belirlenir: Admin formdan seçer, Stajyer kendi kartıdır.
        string? okutulacakKart;
        if (model.AdminMi)
        {
            okutulacakKart = kartNo;
        }
        else
        {
            okutulacakKart = model.KendiKartNo; // formdan gelen değer yok sayılır
        }

        if (string.IsNullOrWhiteSpace(okutulacakKart))
        {
            model.SonucMesaji = model.AdminMi
                ? "Lütfen bir kart seçin."
                : "Size tanımlı bir RFID kartı bulunamadı.";
            model.SonucBasarili = false;
            return View("Index", model);
        }

        var (sonuc, stajyer) = await yoklamaServisi.KartOkutAsync(okutulacakKart);
        var satir = stajyer is null
            ? null
            : await stajyerSorgu.KendiProfilimAsync(stajyer.KullaniciId);

        (model.SonucMesaji, model.SonucBasarili) = sonuc switch
        {
            KartOkutmaSonucu.GirisYapildi =>
                ($"GİRİŞ kaydedildi — {satir?.AdSoyad} ({DateTime.Now:HH:mm})", (bool?)true),
            KartOkutmaSonucu.CikisYapildi =>
                ($"ÇIKIŞ kaydedildi — {satir?.AdSoyad} ({DateTime.Now:HH:mm})", true),
            _ => ("Kart tanınmadı. Kayıtlı bir stajyer kartı değil.", false)
        };

        return View("Index", model);
    }

    /// <summary>Rolü sunucuda çözer: Admin → kart listesi, Stajyer → yalnızca kendi kartı.</summary>
    private async Task<SimulatorViewModel> ModelHazirlaAsync()
    {
        var model = new SimulatorViewModel { AdminMi = User.IsInRole(Roller.Admin) };

        if (model.AdminMi)
        {
            var stajyerler = await stajyerSorgu.TumunuListeleAsync();
            model.KartSecenekleri = stajyerler
                .Select(s => new SelectListItem($"{s.AdSoyad} — Kart {s.Profil.KartNo}", s.Profil.KartNo));
        }
        else if (User.IsInRole(Roller.Stajyer))
        {
            var kullaniciId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var kendi = await stajyerSorgu.KendiProfilimAsync(kullaniciId);
            model.KendiKartNo = kendi?.Profil.KartNo;
            model.KendiAdSoyad = kendi?.AdSoyad;
        }
        // Amir: kartı yoktur — görünüm bilgilendirme gösterir.

        return model;
    }
}
