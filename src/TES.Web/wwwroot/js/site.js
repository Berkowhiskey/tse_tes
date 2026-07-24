// TES — genel istemci betiği.
// Tema (Açık/Koyu) geçişi ve kalıcılığı. İlk uygulama FOUC engeli için layout <head>'inde
// inline yapılır; burada yalnızca düğme davranışı ve senkronizasyon vardır.

(function () {
    "use strict";

    var DEPO_ANAHTARI = "tes-tema"; // localStorage anahtarı: "light" | "dark"

    // Aktif temayı <html data-bs-theme> üzerinden okur.
    function aktifTema() {
        return document.documentElement.getAttribute("data-bs-theme") === "dark" ? "dark" : "light";
    }

    // Temayı uygular ve tercihi saklar.
    function temaUygula(tema) {
        document.documentElement.setAttribute("data-bs-theme", tema);
        try { localStorage.setItem(DEPO_ANAHTARI, tema); } catch (e) { /* depo kapalıysa geç */ }
    }

    function temayiDegistir() {
        temaUygula(aktifTema() === "dark" ? "light" : "dark");
    }

    document.addEventListener("DOMContentLoaded", function () {
        // Navbar'daki tema düğmesi.
        var dugme = document.getElementById("temaDegistir");
        if (dugme) {
            dugme.addEventListener("click", function (e) {
                e.preventDefault();
                temayiDegistir();
            });
        }

        // Kullanıcı tercihi henüz seçmemişse, sistem tercihi değiştiğinde takip et.
        if (window.matchMedia) {
            var mql = window.matchMedia("(prefers-color-scheme: dark)");
            var dinleyici = function (ev) {
                var secilmis = false;
                try { secilmis = !!localStorage.getItem(DEPO_ANAHTARI); } catch (e) { /* geç */ }
                if (!secilmis) {
                    document.documentElement.setAttribute("data-bs-theme", ev.matches ? "dark" : "light");
                }
            };
            if (mql.addEventListener) { mql.addEventListener("change", dinleyici); }
            else if (mql.addListener) { mql.addListener(dinleyici); } // eski tarayıcılar
        }
    });
})();
