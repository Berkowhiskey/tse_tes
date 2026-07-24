# TSE — Takip ve Eğitim Sistemi (TES)

TES; Türk Standardları Enstitüsü (TSE) stajyerlerinin **yoklama/giriş-çıkış takibini**, staj süresi ile kimlik/eğitim bilgilerini, üzerinde çalıştıkları **proje/ödevleri** ve **amir–stajyer ilişkisini** yöneten; ayrıca kurum içi bir **sosyal platform** (gönderi/yorum/beğeni) ve amir–stajyer **iletişimi** (chatbox) sunan bir web uygulamasıdır.

Sistem **on-prem** çalışır ve **yalnızca kurum ağından** erişilir. Stajyerler ağa **TSE-Misafir** üzerinden bağlanır; kurumun asıl işlerinin döndüğü **TSE-Personel** ağıyla hiçbir bağlantı kurulmaz. Erişilemeyen kurum sistemleri (RFID gişeleri, SMTP, personel dizini, firewall) arayüzler arkasında **simüle edilir**.

> Bu bir devlet kurumu (TSE) staj projesidir. Güvenlik ve KVKK kuralları, hız ve pratiklikten önce gelir. Ayrıntılı mimari kararlar için depo kökündeki [CLAUDE.md](CLAUDE.md) dosyasına bakınız.

---

## Roller

| Rol | Özet |
| --- | --- |
| **Admin** | Tüm sistemi yönetir: moderasyon, amir–stajyer/departman eşleştirme, misafir ağı takibi. |
| **Amir** | Kendi stajyerlerinin yoklamasını, bilgilerini, proje/ödevlerini yönetir; misafir erişimine sponsor olabilir. |
| **Stajyer** | Kendi profilini, projesini ve yoklamasını görür; gönderileri admin onayıyla yayınlanır; amiriyle mesajlaşır. |

---

## Teknoloji Yığını

| Katman | Teknoloji |
| --- | --- |
| Web çatısı | ASP.NET Core **9** — **MVC** (Controller + View) |
| Veritabanı | **MSSQL Server** + **EF Core 9** (Code-First + Migrations) |
| Kimlik | **ASP.NET Core Identity** (hash'li parola, rol yönetimi, ilk girişte zorunlu parola değişimi) |
| UI | **Tabler 1.4.0** (Bootstrap 5 tabanlı, MIT, jQuery'siz) — `wwwroot/lib/tabler` altında vendor'lanmış, **CDN kullanılmaz**. Renk teması **scarlet-snow** (paleti `site.css`'te `--tblr-*` override'ı); navbar açılır menülerle gruplu; **Aç/Koyu tema** geçişi (`localStorage` + sistem tercihi, `site.js`) |
| Gerçek zamanlı | **SignalR** (chatbox — amir ↔ stajyer; JS client vendor'lanmış, CDN yok) |
| Test | **xUnit** |

---

## Proje Yapısı

```
TES.sln
├── src/
│   ├── TES.Web/                  # ASP.NET Core MVC — giriş noktası
│   │   ├── Controllers/
│   │   │   ├── HomeController.cs        # Ana sayfa (girişli kullanıcılar) + hata sayfası
│   │   │   └── HesapController.cs       # Giriş / çıkış / zorunlu parola değişimi + denetim kayıtları
│   │   ├── Filters/
│   │   │   └── ParolaDegisimiZorunluFilter.cs  # ForceChangePassword açıkken tüm sayfaları kilitler
│   │   ├── ViewModels/                  # View'lere özel modeller (entity'ler View'e verilmez)
│   │   │   ├── GirisViewModel.cs
│   │   │   └── ParolaDegistirViewModel.cs
│   │   ├── Views/
│   │   │   ├── Shared/_Layout.cshtml    # Tabler ana şablon (navbar, kullanıcı menüsü, responsive)
│   │   │   ├── Shared/_AuthLayout.cshtml# Giriş/parola ekranları için sade ortalanmış şablon
│   │   │   ├── Hesap/                   # Giris, ParolaDegistir, ErisimReddedildi
│   │   │   └── Home/Index.cshtml        # Faz kartlarıyla karşılama panosu
│   │   ├── Politikalar.cs               # Policy adları (AdminPolicy / AmirPolicy / StajyerPolicy)
│   │   ├── Program.cs                   # DbContext, Identity, policy'ler, pipeline, seed çağrısı
│   │   ├── wwwroot/lib/tabler/          # Vendor'lanmış Tabler varlıkları (VERSION.txt ile)
│   │   └── appsettings.Development.example.json  # Örnek yapılandırma (gerçek sırlar user-secrets'ta)
│   │
│   ├── TES.Domain/                # Alan katmanı — dış bağımlılığı YOKTUR
│   │   ├── Entities/DenetimKaydi.cs     # Hassas işlemlerin denetim izi
│   │   ├── Kurallar/KullaniciAdiUretici.cs  # ad_soyad_kartno üretimi + Türkçe→ASCII normalizasyon
│   │   └── Sabitler/Roller.cs           # Admin / Amir / Stajyer rol sabitleri
│   │
│   └── TES.Infrastructure/        # EF Core + Identity + servis implementasyonları
│       ├── Data/
│       │   ├── TesDbContext.cs          # IdentityDbContext + DenetimKayitlari
│       │   ├── VeriTohumlayici.cs       # Roller + örnek kullanıcılar (geliştirme seed'i)
│       │   └── Migrations/              # EF Core migration'ları
│       ├── Identity/Kullanici.cs        # IdentityUser + AdSoyad, ForceChangePassword, AktifMi
│       └── Services/                    # IDenetimServisi / DenetimServisi
│
└── tests/
    └── TES.Tests/
        └── KullaniciAdiUreticiTests.cs  # Kullanıcı adı kuralının birim testleri
```

---

## Kurulum ve Çalıştırma

### Gereksinimler

- .NET SDK **9.0** veya üzeri
- MSSQL Server (yerel instance yeterli)
- `dotnet-ef` global aracı: `dotnet tool install --global dotnet-ef`

### 1. Connection string tanımla (depoya yazılmaz!)

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=.;Database=TES;Trusted_Connection=True;TrustServerCertificate=True" --project src/TES.Web
```

Örnek değerler için: [src/TES.Web/appsettings.Development.example.json](src/TES.Web/appsettings.Development.example.json)

### 2. Derle ve çalıştır

```bash
dotnet build
dotnet watch run --project src/TES.Web
```

Geliştirme ortamında uygulama açılışta **migration'ları otomatik uygular** ve **seed verisini** (roller + örnek kullanıcılar) oluşturur; ayrıca elle uygulamak istersen:

```bash
dotnet ef database update --project src/TES.Infrastructure --startup-project src/TES.Web
```

### 3. Giriş yap

Seed ile gelen örnek kullanıcılar (geçici parola = **sahte** T.C. Kimlik No; ilk girişte parola değişimi **zorunludur**):

| Kullanıcı adı | Rol | Geçici parola (sahte T.C.) |
| --- | --- | --- |
| `admin` | Admin | `10000000146` |
| `mehmet_demir` | Amir | `20000000148` |
| `ayse_yilmaz_1001` | Stajyer | `30000000140` |

> Buradaki T.C. numaraları gerçek değildir; yalnızca geliştirme/seed amaçlı sahte değerlerdir. Parolalar veritabanında yalnızca Identity hash'i olarak tutulur.

### Testler

```bash
dotnet test
```

### Yeni migration eklemek

```bash
dotnet ef migrations add <Ad> --project src/TES.Infrastructure --startup-project src/TES.Web
```

---

## Güvenlik İlkeleri (özet)

- Parolalar **asla düz metin** tutulmaz/loglanmaz — yalnızca Identity hash mekanizması.
- İlk geçici parola T.C. Kimlik No'dur; **ilk girişte değişim zorunludur** (`ForceChangePassword` bayrağı + global filtre).
- Yetki **her zaman sunucuda** doğrulanır (Identity rolleri + policy-based authorization); UI'da gizlemek yeterli sayılmaz.
- Hassas işlemler (giriş, başarısız giriş, kilitlenme, parola değişimi, moderasyon...) `DenetimKaydi` tablosuna yazılır.
- Sırlar (connection string, SMTP vb.) depoya girmez; `dotnet user-secrets` veya ortam değişkeni kullanılır.
- Gerçek ağ erişimi yalnızca `INetworkAccessProvider` arayüzü üzerinden verilir (MVP'de simülasyon).

## TSE-Misafir Akışı (Faz 2)

1. **Talep:** Anonim portal (`/Misafir/Portal`) veya stajyerin "Taleplerim" sayfası — Ad Soyad, e-posta, sponsor `@tse.org.tr` adresi (mock personel dizininde doğrulanır), süre 1/3/5 gün. Sponsora saatlik rate-limit uygulanır.
2. **Onay bekleme:** Talep sahibi "Account Status" sayfasına yönlenir (30 sn'de bir otomatik yenilenir). Sponsora **tek kullanımlık, süreli, kriptografik** token'lı onay bağlantısı e-postalanır (mock SMTP → Admin'in "Giden E-postalar (Sim)" ekranı).
3. **Karar:** Sponsor onaylarsa talep `Enabled` olur, ağ erişimi `SimulatedNetworkAccessProvider` üzerinden açılır ve **voucher** (yalnızca hash'i saklanır) talep sahibine e-postalanır. Reddederse `Denied`; süresinde yanıtlamazsa arka plan servisi talebi **otomatik iptal** eder (`Expired`).
4. **Cihaz ekleme:** `/Misafir/Voucher` sayfasından voucher koduyla ek cihazlar bağlanır (cihaz limiti yapılandırılabilir).
5. **Yönetim:** Admin tüm talepleri, karar + enforcement durumunu yan yana izler; manuel onay/iptal yapabilir.

> Mock sponsor dizini: `mehmet.demir@`, `fatma.kaya@`, `ali.ozkan@`, `zeynep.arslan@` (tse.org.tr). Ayarlar `appsettings.json → MisafirAyarlari` (token süresi MVP testinde 15 dk).

---

## Yol Haritası

- [x] **Faz 0 — İskelet:** proje kurulumu, Identity, Tabler layout, DbContext + ilk migration, seed
- [x] **Faz 1 — Kimlik & organizasyon:** roller/profiller, Departman hiyerarşisi, Amir–Stajyer eşleştirme, Yoklama (RFID simülatörü)
- [x] **Faz 2 — Misafir ağı (kalp):** TSE-Misafir sponsor akışı, SMTP simülasyonu, `SimulatedNetworkAccessProvider`
- [x] **Faz 3 — İş takibi:** Proje & Ödev takibi (amir atar, stajyer ilerleme raporlar)
- [x] **Faz 4 — Sosyal:** Gönderi + moderasyon + Yorum/Beğeni
- [x] **Faz 5 — İletişim:** Chatbox + bildirimler (SignalR)
- [x] **Faz 6 — Görünüm:** Responsive navbar (açılır menü gruplama), scarlet-snow renk teması + Aç/Koyu geçiş
- [ ] **Sonrası:** mobil push, basit yerel AI, KVKK sertleştirme
