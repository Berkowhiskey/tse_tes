using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TES.Infrastructure.Services;
using TES.Web.ViewModels;

namespace TES.Web.Controllers;

// GERÇEKTE: Bu ekranın yerini kurum girişindeki fiziksel RFID gişeleri alır; gişe cihazı
// kart okuttuğunda aynı IYoklamaServisi.KartOkutAsync akışı tetiklenir. Simülatör yalnızca
// geliştirme/demoda kart okutma olayını üretir (CLAUDE.md Bölüm 9).
/// <summary>RFID yoklama simülatörü — kart seç → giriş/çıkış. Yalnızca Admin kullanır.</summary>
[Authorize(Policy = Politikalar.AdminPolicy)]
public class YoklamaSimulatorController(
    IStajyerSorguServisi stajyerSorgu,
    IYoklamaServisi yoklamaServisi) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new SimulatorViewModel { KartSecenekleri = await KartSecenekleriAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KartOkut(string kartNo)
    {
        var model = new SimulatorViewModel { KartSecenekleri = await KartSecenekleriAsync() };

        if (string.IsNullOrWhiteSpace(kartNo))
        {
            model.SonucMesaji = "Lütfen bir kart seçin.";
            model.SonucBasarili = false;
            return View("Index", model);
        }

        var (sonuc, stajyer) = await yoklamaServisi.KartOkutAsync(kartNo);
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

    private async Task<IEnumerable<SelectListItem>> KartSecenekleriAsync()
    {
        var stajyerler = await stajyerSorgu.TumunuListeleAsync();
        return stajyerler.Select(s => new SelectListItem($"{s.AdSoyad} — Kart {s.Profil.KartNo}", s.Profil.KartNo));
    }
}
