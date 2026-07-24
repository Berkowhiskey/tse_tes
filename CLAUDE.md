# CLAUDE.md

Bu dosya, **TSE - Takip ve Eğitim Sistemi (TES)** deposunda çalışan Claude Code için pusuladır. Her oturumda okunur; mimari, güvenlik, alan modeli ve kodlama kararlarını bağlayıcı biçimde tanımlar. Ayrıntılı gerekçeler için Notion'daki **"TES — Proje Künyesi & Karar Kaydı"** sayfasına bakılır. Bu dosyayla künye çeliştiğinde **bu dosya esastır**; kararı güncellerken her ikisini de güncelle.

> Bu bir devlet kurumu (TSE) staj projesidir. Güvenlik ve KVKK kuralları, hız ve pratiklikten **önce** gelir. Bir talep bu dosyadaki kurallarla çelişiyorsa, uygulamadan önce dur ve kullanıcıya sor.

---

## 1. Proje Özeti

TES; TSE stajyerlerinin yoklama/giriş-çıkış takibini, staj süresi ile kimlik/eğitim bilgilerini, üzerinde çalıştıkları proje/ödevleri ve amir–stajyer ilişkisini yöneten; ayrıca kurum içi bir sosyal platform (gönderi/yorum/beğeni) ve amir–stajyer iletişimi (chatbox) sunan bir web uygulamasıdır.

Sistem **on-prem** çalışır ve **yalnızca kurum ağından** erişilir. Stajyerler bu ağa **TSE-Misafir** üzerinden bağlanır. Kurumun asıl işlerinin döndüğü **TSE-Personel** ağıyla **hiçbir** bağlantı kurulmaz.

Üç rol vardır: **Admin**, **Amir**, **Stajyer** (yetkiler için Bölüm 5).

---

## 2. Teknoloji Yığını

- **.NET / Framework:** ASP.NET Core **9**, **MVC** (Controller + View). Tek ve tutarlı yaklaşım — Razor Pages ile karıştırma.
- **Veritabanı:** MSSQL Server, **EF Core** (Code-First + Migrations).
- **Kimlik:** **ASP.NET Core Identity** (hash'li parola, rol yönetimi, ilk girişte zorunlu parola değişimi).
- **UI şablonu:** **Tabler** (Bootstrap 5, MIT, jQuery'siz). `wwwroot/lib/` altında vendor'lanır, `_Layout.cshtml` üzerinden bağlanır. Arayüz **mobile responsive** olmalı.
- **Gerçek zamanlı:** **SignalR** — chatbox + bildirimler.
- **E-posta:** Sponsor onayı için sistem içi SMTP modülü (MVP'de simülasyon; Bölüm 8).

Sürüm kararı verirken tahmin etme; belirsizse kullanıcıya sor veya resmi dokümana bak.

---

## 3. Mimari İlkeler

1. **Ağdan izolasyon.** Kod, hiçbir koşulda TSE-Personel ağına ait bir kaynağa bağlanmaz/referans vermez. Tüm dış bağımlılıklar açıkça tanımlı ve simüle edilebilir olmalı.
2. **İki katman ayrımı (misafir ağı).** Uygulama yalnızca **karar/iş akışı** katmanını yönetir (form, onay, durum, süre, voucher, log). Paketlere gerçekten izin veren **enforcement** katmanı ayrıdır ve `INetworkAccessProvider` arayüzünün arkasındadır. MVP'de `SimulatedNetworkAccessProvider` kullanılır; gerçek firewall/RADIUS çağrısı **asla** doğrudan yazılmaz.
3. **Gerçek sistemler arayüz arkasında simüle edilir.** Erişemediğimiz her kurum sistemi (RFID, SMTP, personel dizini) bir arayüzle soyutlanır ve simülasyon implementasyonu yazılır. Her simülasyon bileşeninin başına **`// GERÇEKTE: ...`** yorumu eklenir (staj raporu için, Bölüm 8).
4. **Denetlenebilirlik.** Hassas her işlem (giriş, rol/eşleştirme değişikliği, moderasyon, misafir onayı, veri görüntüleme) `DenetimKaydi`'na yazılır.
5. **Küçük ve gözden geçirilebilir artımlar.** İş, faz planına (Bölüm 12) göre ilerler; her adımda çalışan bir şey bırakılır.

---

## 4. Proje Yapısı (öneri — değiştirilebilir)

Tek ASP.NET Core MVC projesiyle başla; büyürse katmanlara ayrılır.

```
/src
  /TES.Web                 # ASP.NET Core MVC (giriş noktası)
    /Controllers
    /Views
    /ViewModels            # View'lere özel modeller (entity'leri doğrudan View'e verme)
    /Hubs                  # SignalR (ChatHub, NotificationHub)
    /wwwroot/lib/tabler    # vendor'lanmış Tabler varlıkları
  /TES.Domain              # Entity'ler, enum'lar, alan kuralları (bağımlılıksız)
  /TES.Infrastructure      # EF Core DbContext, Migrations, servis implementasyonları
    /Data
    /Simulation            # SimulatedNetworkAccessProvider, RFID simülatörü, mock SMTP/dizin
/tests
  /TES.Tests
```

Erken aşamada tek proje de kabul; ama yukarıdaki klasör ayrımını koru.

---

## 5. Roller ve Yetkilendirme

Yetkilendirme **Identity rolleri + policy-based authorization** ile yapılır. Controller/action'larda `[Authorize(Policy = "...")]` kullan; yetki mantığını View'e gömme. "Kendi stajyeri" / "kendi içeriği" gibi sahiplik kontrolleri **sunucu tarafında** doğrulanır (sadece UI'da gizlemek yetmez).

| Özellik | Admin | Amir | Stajyer |
| --- | --- | --- | --- |
| Gönderi oluşturma | ✔ (onaysız) | ✔ (onaysız) | ✔ (**admin onayına tabi**) |
| Gönderi moderasyonu (onay/ret + red mesajı) | ✔ | ✘ (görür, yardımcı olur) | ✘ |
| İçerik kaldırma (gönderi **ve** yorum) | ✔ (herkesin, her koşulda) | ✔ (yalnız kendi) | ✔ (yalnız kendi) |
| Yorum & beğeni | ✔ | ✔ (kısıtsız) | ✔ (kısıtsız) |
| Yoklama görüntüleme | ✔ (tümü) | ✔ (kendi stajyerleri) | ✔ (yalnız kendisi) |
| Stajyer bilgileri / staj süresi | ✔ (tümü) | ✔ (kendi stajyerleri) | ✔ (kendi profili) |
| Ödev atama | ✔ | ✔ (kendi stajyerlerine) | görüntüler |
| Proje / görev takibi | ✔ | ✔ (kendi stajyerleri) | ✔ (kendi projesi) |
| Amir–Stajyer & Departman eşleştirme | ✔ | ✘ | ✘ |
| Chatbox | izler | ✔ (kendi stajyeriyle) | ✔ (amiriyle) |
| TSE-Misafir | manuel bağlama + takip | sponsor olabilir | talep gönderir |
| Profil düzenleme | — | ✔ (kendi) | ✔ (kendi) |

**Kritik:** Stajyer gönderileri onaylanana kadar yayında görünmez. Amir gönderileri onaydan muaftır. Admin, sahibi kim olursa olsun her gönderiyi/yorumu kaldırabilir. Kullanıcı kendi gönderi/yorumunu silebilir.

---

## 6. Alan Modeli (Ubiquitous Language)

**İsimlendirme dili:** Alan (domain) tipleri **ASCII'ye normalize edilmiş Türkçe** adlarla yazılır (Türkçe karakter kullanma: `Odev`, `Gonderi`, `Begeni`). Altyapı/teknik soyutlamalar İngilizce olabilir (ör. `INetworkAccessProvider`). Bu tutarlılığı koru.

Ana entity'ler ve kilit alanları:

- **Kullanici** (Identity tabanlı) — roller: `Admin` / `Amir` / `Stajyer`.
- **Departman** — kendine referanslı hiyerarşi (`UstDepartmanId`). Örn: Bilgi-İşlem → { Yazılım Geliştirme, Sistem, Donanım }. Amirin departmanı = stajyerin departmanı.
- **AmirProfil** — `IseBaslamaTarihi`, `Hakkimda`, `DepartmanId`.
- **StajyerProfil** — kimlik/eğitim bilgileri, `KartNo`, `StajBaslangic`, `StajBitis`, `AmirId` (**tek amir**), `DepartmanId` (amirden gelir).
- **Proje** — bir stajyere ait (**stajyer başına tek proje**; payla­şımlı senaryo ilerideyse çoka-çok'a genişletilir), `Durum`, `Ilerleme`.
- **Odev** — `AmirId → StajyerId`, `Aciklama`, `Durum`, `Ilerleme`, `TeslimTarihi`.
- **Gonderi** — `YazarId`, `Icerik`, `ModerasyonDurumu` (`Beklemede`/`Onaylandi`/`Reddedildi`), `RedMesaji`, zaman damgaları.
- **Yorum** — `GonderiId`, `YazarId`, `Icerik`.
- **Begeni** — `GonderiId`, `KullaniciId`.
- **YoklamaKaydi** — `StajyerId`/`KartNo`, `GirisZamani`, `CikisZamani`. **Kural:** aynı gün içindeki giriş-çıkış çiftleri eşleştirilir; eşi olmayan giriş "açık oturum" sayılır.
- **SohbetMesaji** — `GondericiId`, `AliciId` (amir ↔ stajyer), `Icerik`, `Zaman`.
- **MisafirErisimTalebi** — opsiyonel `StajyerId` (ya da anonim misafir), `AdSoyad`, `Eposta`, `SponsorEposta`, `Sure` (1/3/5 gün), `Token`, `Durum` (`Beklemede`/`Enabled`/`Expired`/`Denied`), `VoucherHash`, `CihazId`.
- **DenetimKaydi** — `Aktor`, `Islem`, `Zaman`, `Detay`.

---

## 7. Güvenlik ve KVKK Kuralları (bağlayıcı)

- **Parola asla düz metin tutulmaz.** Yalnızca Identity'nin hash mekanizması kullanılır. Parolalar loglanmaz, ekrana yazılmaz.
- **İlk (geçici) parola = T.C. Kimlik No.** İlk girişte parola değişimi **zorunludur** (`ForceChangePassword` bayrağı). Değişimden sonra kullanıcı serbestçe değiştirebilir.
- **Kullanıcı adı** `ad_soyad_kartno` üretilirken Türkçe karakterler normalize edilir; isim çakışması ve boşluk deterministik biçimde çözülür.
- **Denetim kaydı:** hassas işlemler `DenetimKaydi`'na yazılır.
- **Yetki her zaman sunucuda doğrulanır.** Sahiplik ("kendi stajyeri/içeriği") action seviyesinde kontrol edilir.
- **Sırlar depoya girmez.** Connection string, SMTP kimlik bilgileri vb. `appsettings.json`'a **yazılmaz**; `dotnet user-secrets` veya ortam değişkeni kullanılır. Örnek değerler için `appsettings.Development.example.json` bırak.
- **KVKK (MVP'ye teknik ulaşınca derinleştirilecek — şimdilik iskele + notlar):** hassas alanların şifreli saklanması, veri saklama süreleri, staj bitince hesap pasifleştirme/anonimleştirme. Bu alanları modele koy ama sertleştirmeyi faza bağla.

> **Açık risk (kabul edildi):** T.C.'nin ilk parola olması KVKK zafiyetidir; MVP kolaylığı için kabul edildi. Bunu genişletme veya başka yere sızdırma; yalnızca ilk giriş için, hash'li, zorunlu değişimle kullan.

---

## 8. TSE-Misafir Sponsor Akışı ("sponsored guest access")

İki giriş kapısı: (a) henüz ağa girmemiş kişi için anonim portal, (b) TES'e girmiş stajyer için "talep et/uzat". Admin manuel bağlayabilir.

Akış: bağlan → form (Ad-Soyad, E-posta, sponsor `@tse.org.tr` e-postası, süre 1/3/5) → sponsora tek kullanımlık süreli token'lı e-posta → kullanıcıda 30 sn'de yenilenen "Account Status" sayfası (+ çok cihaz için voucher) → sponsor onayı → `Enabled` → Login.

**Uygulama kuralları:**
- Sponsor e-postası **serbest yazılır**, ama gönderim öncesi **kayıtlı bir `@tse.org.tr` hesabı olduğu doğrulanır** (mock personel dizini arayüzü). Olmayan adres reddedilir.
- Onay token'ı **tek kullanımlık, tahmin edilemez (kriptografik), süreli**. Prod'da 1–2 saat; **MVP testinde kısa** (yapılandırılabilir olsun).
- Sponsor süresinde yanıt vermezse talep **otomatik iptal**.
- **Rate-limit** uygula (bir sponsora istek yağmuru engellenir).
- **Voucher:** rastgele üretilir, **hash'lenir**, seçilen süre sonunda geçersizleşir, iptal edilebilir; cihaz limiti düşünülebilir.
- Gerçek ağ erişimi **yalnızca** `INetworkAccessProvider` üzerinden verilir (MVP'de simüle).

---

## 9. Simülasyon Bileşenleri

Erişemediğimiz her kurum sistemi bir arayüzle soyutlanır; simülasyon implementasyonunun başına **`// GERÇEKTE: ...`** yorumu konur.

| Arayüz / Bileşen | MVP implementasyonu | GERÇEKTE |
| --- | --- | --- |
| `INetworkAccessProvider` | `SimulatedNetworkAccessProvider` (durum: `Pending→Enabled→Expired`) | FortiGate/Firewall guest portal + RADIUS CoA |
| RFID yoklama | Basit web ekranı (kart seç → giriş/çıkış) veya script | Kurum girişindeki fiziksel RFID gişeleri |
| `IEmailSender` (sponsor) | Mock SMTP (konsola/DB'ye yazar) | Kurumun kimlik doğrulamalı SMTP relay'i (SPF/DKIM) |
| `IPersonelDizini` | Sabit/mock `@tse.org.tr` listesi | Kurumun AD/LDAP dizini |
| İK stajyer–kart eşleşmesi | Seed veri / örnek Excel | İK'nın tuttuğu gerçek Excel |

---

## 10. Kodlama Standartları

- **C#:** nullable reference types **açık**; `async/await` tüm I/O'da; erken dönüş (guard clause) tercih et; anlamlı isimler.
- **EF Core:** Code-First. Şema değişikliği = yeni migration. Ham SQL yerine LINQ; performans için gerektiğinde projection (`Select`) kullan. Entity'leri doğrudan View'e verme, **ViewModel** kullan.
- **Controller'lar ince, iş mantığı servislerde.** Servisler arayüzle tanımlanıp DI ile enjekte edilir.
- **Doğrulama:** Model doğrulaması (DataAnnotations/FluentValidation) sunucu tarafında; `ModelState` kontrol edilir.
- **Razor:** Tabler bileşenlerini kullan; anti-forgery token'ları formlarda aktif; kullanıcı içeriği daima encode edilir (XSS).
- **SignalR:** Hub metotlarında da yetki kontrolü yap; grup/kullanıcı bazlı mesajlaşmada alıcı doğrulanır.
- **Loglama:** `ILogger` kullan; kişisel veri/parola/token loglama.
- **Yorumlar ve UI metinleri Türkçe;** kod tanımlayıcıları Bölüm 6'daki dile uyar.

---

## 11. Komutlar

```bash
# Derleme / çalıştırma
dotnet build
dotnet watch run --project src/TES.Web

# EF Core migration
dotnet ef migrations add <Ad> --project src/TES.Infrastructure --startup-project src/TES.Web
dotnet ef database update --project src/TES.Infrastructure --startup-project src/TES.Web

# Testler
dotnet test

# Sırlar (connection string vb. — depoya YAZMA)
dotnet user-secrets set "ConnectionStrings:Default" "Server=.;Database=TES;Trusted_Connection=True;TrustServerCertificate=True"
```

---

## 12. Faz Planı / Çalışma Düzeni

Sıralama esnektir; her fazın sonunda çalışan bir şey bırak.

- **Faz 0 — İskelet:** proje kurulumu, Identity, Tabler layout, DbContext + ilk migration, seed (roller + örnek kullanıcılar).
- **Faz 1 — Kimlik & organizasyon:** roller/profil, Departman hiyerarşisi, Amir–Stajyer eşleştirme, Yoklama (RFID simülatörü).
- **Faz 2 — Misafir ağı (kalp):** TSE-Misafir sponsor akışı; SMTP simülasyonu; `SimulatedNetworkAccessProvider`.
- **Faz 3 — İş takibi:** Proje & Odev takibi.
- **Faz 4 — Sosyal:** Gonderi + moderasyon + Yorum/Begeni.
- **Faz 5 — İletişim:** Chatbox + bildirimler (SignalR).
- **Sonrası:** mobil (responsive/ayrı uygulama), basit yerel AI, KVKK sertleştirme.

---

## 13. Kesin Kurallar — YAPMA

- TSE-Personel ağına bağlanan/atıf yapan kod **yazma**.
- Parolayı/T.C.'yi/token'ı **düz metin saklama, loglama, ekrana yazma**.
- Gerçek firewall/RADIUS'u doğrudan **çağırma**; her ağ erişimi `INetworkAccessProvider`'dan geçer.
- Sırları `appsettings.json`'a veya depoya **koyma**.
- Yetkiyi yalnızca UI'da **gizleme**; sunucuda doğrula.
- Stajyer gönderisini onaysız **yayına alma**.
- Simülasyon yazarken `// GERÇEKTE: ...` yorumunu **atlama**.
- Bu dosyayla veya Notion künyesiyle çelişen bir talepte, uygulamadan önce **dur ve sor**.

---

## 14. "Bitti" Tanımı

Bir özellik; yetkilendirmesi sunucuda doğrulanmış, doğrulama/hata durumları ele alınmış, gerekli denetim kayıtları eklenmiş, en az temel testleri yazılmış, `dotnet build` ve `dotnet test` temiz geçiyor ve mobil görünümde bozulmuyorsa tamamlanmış sayılır.

---

## 15. İlerleme Günlüğü

> Her önemli adım buraya not düşülür. Biçim: **Durum [Tarih Saat]** — açıklama.

### Faz 0 — İskelet · **Tamamlandı [22.07.2026 16:07]**

- **Tamamlandı [22.07.2026 15:42]** — Solution + proje yapısı kuruldu: `TES.Web` (MVC), `TES.Domain` (bağımlılıksız), `TES.Infrastructure` (EF Core + Identity), `TES.Tests` (xUnit). Referanslar ve NuGet paketleri (EF Core / Identity 9.0.18) bağlandı.
- **Tamamlandı [22.07.2026 15:45]** — Domain katmanı: `Roller` sabitleri, `DenetimKaydi` entity'si, `KullaniciAdiUretici` (ad_soyad_kartno, Türkçe→ASCII normalizasyon, İ/I ayrımı, deterministik çakışma çözümü).
- **Tamamlandı [22.07.2026 15:47]** — Infrastructure katmanı: `Kullanici` (IdentityUser + `AdSoyad`, `ForceChangePassword`, `AktifMi`), `TesDbContext`, `IDenetimServisi`/`DenetimServisi`, `VeriTohumlayici` (roller + 3 örnek kullanıcı, geçici parola = sahte T.C., hash'li).
- **Tamamlandı [22.07.2026 15:49]** — Web katmanı: `Program.cs` (Identity, cookie, policy'ler: `AdminPolicy`/`AmirPolicy`/`StajyerPolicy`, lockout 5 deneme→5 dk), `HesapController` (giriş/çıkış/parola değişimi + denetim kayıtları), `ParolaDegisimiZorunluFilter` (global — ilk girişte parola değişimi zorunlu).
- **Tamamlandı [22.07.2026 15:50]** — Tabler 1.4.0 `wwwroot/lib/tabler` altına vendor'landı (CDN yok); `_Layout.cshtml` + `_AuthLayout.cshtml` responsive olarak kuruldu; şablonun bootstrap/jquery kalıntıları temizlendi.
- **Tamamlandı [22.07.2026 15:50]** — Sırlar: connection string `dotnet user-secrets`'a taşındı; depoya yalnızca `appsettings.Development.example.json` kondu.
- **Tamamlandı [22.07.2026 15:51]** — İlk migration (`IlkOlusturma`) oluşturuldu ve `TES` veritabanına uygulandı; seed doğrulandı (3 kullanıcı + roller + denetim kaydı).
- **Tamamlandı [22.07.2026 15:52]** — Testler: `KullaniciAdiUreticiTests` (11/11 geçti). Duman testi: `/Hesap/Giris` 200, anonim `/` → login 302. `dotnet build` ve `dotnet test` temiz.
- **Tamamlandı [22.07.2026 16:07]** — Kullanıcı doğrulaması: 3 seed kullanıcısıyla giriş yapıldı, zorunlu parola değişim akışı uçtan uca çalıştı. `README.md` hazırlandı, `.gitignore` eklendi.

### Faz 1 — Kimlik & organizasyon · **Tamamlandı [22.07.2026 16:47]**

- **Tamamlandı [22.07.2026 16:20]** — Domain entity'leri: `Departman` (kendine referanslı hiyerarşi), `AmirProfil`, `StajyerProfil` (tek amir, departman amirden; T.C. SAKLANMAZ), `YoklamaKaydi` (açık oturum kuralı).
- **Tamamlandı [22.07.2026 16:25]** — `TesDbContext` ilişkileri: benzersiz indeksler (KullaniciId, KartNo), hiyerarşide Restrict, amir silinince `ClientSetNull` (SQL Server çoklu cascade yolu engeli), migration `Faz1KimlikOrganizasyon` oluşturuldu ve uygulandı.
- **Tamamlandı [22.07.2026 16:30]** — Servisler: `DepartmanServisi` (hiyerarşik liste, döngü koruması, dolu departman silinemez), `KullaniciYonetimServisi` (amir/stajyer hesabı açma — geçici parola=T.C., saklanmaz; eşleştirmede departman amirden), `StajyerSorguServisi` (sahiplik kuralı sunucuda), `YoklamaServisi` (gün içi giriş-çıkış eşleştirme), `ProfilServisi`. Hepsi DI ile kayıtlı.
- **Tamamlandı [22.07.2026 16:35]** — Seed genişletildi: Bilgi-İşlem → {Yazılım Geliştirme, Sistem, Donanım}; mehmet_demir → AmirProfil (Yazılım Geliştirme); ayse_yilmaz_1001 → StajyerProfil (kart 1001, amiri mehmet_demir). İdempotent.
- **Tamamlandı [22.07.2026 16:40]** — Web ekranları: Departmanlar (Admin CRUD), Kullanıcı Yönetimi (amir/stajyer oluşturma + Eşleştir), Stajyerler (Amir kendi / Admin tümü; detay görüntüleme `StajyerVerisiGoruntulendi` denetim kaydı yazar), Profilim (görüntüle/düzenle), Yoklama (role göre kapsam), rol bazlı navbar.
- **Tamamlandı [22.07.2026 16:42]** — RFID Yoklama Simülatörü (`// GERÇEKTE: fiziksel RFID gişeleri` yorumuyla, Admin'e açık): kart seç → aynı gün açık oturum varsa çıkış, yoksa giriş.
- **Tamamlandı [22.07.2026 16:47]** — Testler: SQLite in-memory ile `YoklamaServisi` (5 test: tanınmayan kart, giriş, gün içi çift eşleşme, üçüncü okutma, dünkü açık oturum korunur) + `DepartmanServisi` (4 test). Toplam **20/20 geçti**. Duman testi: uygulama açıldı, Faz 1 seed DB'de doğrulandı. `dotnet build` + `dotnet test` temiz.

### Faz 1.5 — Küçük iyileştirmeler · **Tamamlandı [23.07.2026 08:53]**

- **Tamamlandı [23.07.2026 08:53]** — Dashboard kartları tıklanabilir ve rol-duyarlı yapıldı (Yoklama, RFID Simülatör, Stajyerler, Departmanlar, Kullanıcı Yönetimi, Profilim; Faz 2/3 kartları yer tutucu). RFID Simülatörü stajyere açıldı: stajyer YALNIZCA kendi kartını okutabilir — kart sunucuda kimlikten çözülür, formdan gelen değer yok sayılır; Admin tüm kartları okutmaya devam eder; Amir'e "kartınız yok" bilgisi gösterilir. Navbar'da simülatör linki Admin+Stajyer'e görünür. Build + 20/20 test temiz.

### Faz 2 — Misafir ağı (kalp) · **Tamamlandı [23.07.2026 09:21]**

- **Tamamlandı [23.07.2026 09:00]** — Domain: `MisafirErisimTalebi` (TakipKodu Guid, opsiyonel StajyerProfilId, `MisafirTalepDurumu`: Beklemede/Enabled/Expired/Denied). Token ve voucher düz metin SAKLANMAZ — yalnızca SHA-256 hash (`TokenHash`, `VoucherHash`).
- **Tamamlandı [23.07.2026 09:05]** — Simülasyon bileşenleri (hepsi `// GERÇEKTE:` yorumuyla): `IEmailSender`/`MockEmailSender` (teslimat kanalı = `GidenEposta` tablosu; loga içerik yazılmaz), `IPersonelDizini`/`MockPersonelDizini` (sabit @tse.org.tr listesi), `INetworkAccessProvider`/`SimulatedNetworkAccessProvider` (enforcement ayrı `SimuleAgErisimi` tablosunda: Pending→Enabled→Expired + cihaz sayacı).
- **Tamamlandı [23.07.2026 09:08]** — `TokenYardimcisi` (256-bit URL-güvenli token, okunabilir voucher XXXX-XXXX-XXXX, SHA-256) + `MisafirAyarlari` (appsettings: token 15 dk — MVP testi için kısa, prod'da uzatılır; sponsor saatlik limit 3; cihaz limiti 3; süreler 1/3/5 gün).
- **Tamamlandı [23.07.2026 09:10]** — `MisafirTalepServisi`: sponsor dizin doğrulaması, rate-limit, tek kullanımlık süreli token, onayda voucher üretimi + ağ erişimi arayüzden açma, red, admin manuel onay/iptal, voucher ile cihaz ekleme (limitli), `SuresiGecenleriIsleAsync` (yanıtsız → otomatik iptal; süresi biten Enabled → kapatma). `MisafirTemizlikServisi` (BackgroundService, 60 sn) + migration `Faz2MisafirAgi`.
- **Tamamlandı [23.07.2026 09:15]** — Web: anonim Portal, Account Status (30 sn meta refresh, takip kodu Guid ile), sponsor Onay/Red ekranı (tek kullanımlık bağlantı), Voucher cihaz ekleme, stajyer "Taleplerim" (ad-soyad ve stajyer bağı sunucuda kimlikten), Admin "Misafir Yönetimi" (karar + enforcement durumu yan yana, manuel onay/iptal) ve "Giden E-postalar (Sim)". Navbar + dashboard + giriş sayfası bağlantıları.
- **Tamamlandı [23.07.2026 09:21]** — Testler: `MisafirTalepServisiTests` (11 test: dizin dışı sponsor, tse dışı adres, hash'li saklama, rate-limit, onay→Enabled+ağ+voucher, tek kullanımlık token, red, voucher cihaz limiti, otomatik iptal, süre dolumu→ağ kapatma, manuel onay). Toplam **31/31 geçti**. Uçtan uca canlı duman testi: portal→talep→sponsor e-postası→onay→Enabled→voucher ile cihaz ekleme→kullanılmış token reddi. `dotnet build` + `dotnet test` temiz.

- **Tamamlandı [23.07.2026 14:58]** — Hata düzeltme: stajyerin "Taleplerim" formu sessizce başarısız oluyordu — form, `[Required] AdSoyad` içeren `MisafirTalepFormViewModel`'i kullanıyor ama AdSoyad alanı formda yok (sunucuda kimlikten alınıyor); doğrulama her POST'ta patlıyordu. Çözüm: AdSoyad içermeyen ayrı `StajyerTalepFormViewModel` oluşturuldu; `TalepEt` action'ı ve `TaleplerimViewModel` buna geçirildi. Build + 31/31 test temiz.

### Faz 2.5 — Misafir akışı iyileştirmeleri · **Tamamlandı [24.07.2026 09:36]**

- **Tamamlandı [24.07.2026 09:36]** — İki iyileştirme: (1) Account Status (Durum) sayfasına, YALNIZCA giriş yapmış kullanıcıya görünen "Ana Sayfaya Dön" butonu eklendi (anonim misafire gösterilmez). (2) Amir sponsor onayı: sponsor gösterilen amir, talebi sistem içinden onaylayabilir/reddedebilir — admin tekeli kalktı. Seed'de amire e-posta bağlandı (mehmet_demir → mehmet.demir@tse.org.tr); `MisafirTalepServisi.SponsorunTalepleriAsync` + `SponsorKararVerAsync` (sahiplik SUNUCUDA: `talep.SponsorEposta == amir.Email`, case-insensitive); `MisafirOnayController` + "Sponsor Onaylarım" ekranı (yalnız Amir rolü); navbar + dashboard linkleri. Testler: 2 yeni (doğru sponsor onaylar → Enabled; başka sponsor reddedilir → Beklemede kalır). Toplam **33/33 geçti**. Duman: amir e-postası seed'le doğrulandı, /MisafirOnay anonim erişimde login'e 302.

- **Tamamlandı [24.07.2026 10:01]** — Yeni amirlere otomatik kurum e-postası: `KullaniciYonetimServisi.AmirOlusturAsync` artık kullanıcı adından `ad.soyad@tse.org.tr` üretip atıyor (`EpostaUret`: `deneme_amiri` → `deneme.amiri@tse.org.tr`); başarı mesajında e-posta gösteriliyor. `MockPersonelDizini` DbContext'e bağlandı — sabit @tse.org.tr listesine EK olarak sistemdeki amir e-postalarını da tanıyor (aksi halde yeni amir sponsor gösterilince talep en baştan reddediliyordu). Seed'e `AmirEpostalariniDoldurAsync` (idempotent) eklendi: e-postası eksik tüm amirler — mevcut "Deneme Amiri" dahil — otomatik dolduruldu. Testler: 6 yeni (`MockPersonelDiziniTests`: EpostaUret, sabit/tse-dışı/sistemdeki-amir/hayalet adres). Toplam **39/39 geçti**. Duman: Deneme Amiri (deneme.amiri@tse.org.tr) sponsor gösterilerek talep oluşturulabildi — önceki reddedilen senaryo artık çalışıyor.

### Faz 3 — İş takibi · **Sırada**

- Kapsam: `Proje` ve `Odev` takibi (amir → stajyer atama, durum/ilerleme).
