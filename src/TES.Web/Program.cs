using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TES.Domain.Sabitler;
using TES.Infrastructure.Data;
using TES.Infrastructure.Identity;
using TES.Infrastructure.Services;
using TES.Infrastructure.Simulation;
using TES.Web;
using TES.Web.Filters;
using TES.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı — connection string user-secrets veya ortam değişkeninden gelir (depoya yazılmaz).
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "'ConnectionStrings:Default' bulunamadı. 'dotnet user-secrets set' ile tanımlayın " +
        "(bkz. appsettings.Development.example.json).");

builder.Services.AddDbContext<TesDbContext>(o => o.UseSqlServer(connectionString));

// Identity — parolalar yalnızca hash'li saklanır.
builder.Services
    .AddIdentity<Kullanici, IdentityRole>(o =>
    {
        // İlk geçici parola 11 haneli T.C. olduğundan politika rakam-ağırlıklı paroları kabul eder;
        // kullanıcı ilk girişte kendi parolasını belirlemek ZORUNDADIR (ForceChangePassword).
        o.Password.RequiredLength = 8;
        o.Password.RequireDigit = true;
        o.Password.RequireUppercase = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireNonAlphanumeric = false;

        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<TesDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Hesap/Giris";
    o.LogoutPath = "/Hesap/Cikis";
    o.AccessDeniedPath = "/Hesap/ErisimReddedildi";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
});

// Policy tabanlı yetkilendirme — controller'larda [Authorize(Policy = ...)] ile kullanılır.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politikalar.AdminPolicy, p => p.RequireRole(Roller.Admin))
    .AddPolicy(Politikalar.AmirPolicy, p => p.RequireRole(Roller.Admin, Roller.Amir))
    .AddPolicy(Politikalar.StajyerPolicy, p => p.RequireRole(Roller.Admin, Roller.Amir, Roller.Stajyer));

// Servisler
builder.Services.AddScoped<IDenetimServisi, DenetimServisi>();
builder.Services.AddScoped<IDepartmanServisi, DepartmanServisi>();
builder.Services.AddScoped<IKullaniciYonetimServisi, KullaniciYonetimServisi>();
builder.Services.AddScoped<IStajyerSorguServisi, StajyerSorguServisi>();
builder.Services.AddScoped<IYoklamaServisi, YoklamaServisi>();
builder.Services.AddScoped<IProfilServisi, ProfilServisi>();
builder.Services.AddScoped<IProjeServisi, ProjeServisi>();
builder.Services.AddScoped<IOdevServisi, OdevServisi>();
builder.Services.AddScoped<ISosyalServis, SosyalServis>();

// TSE-Misafir (Faz 2): ayarlar + simülasyon bileşenleri + karar servisi.
// Gerçek sistemler arayüz arkasında simüle edilir (CLAUDE.md Bölüm 3 ve 9).
builder.Services.Configure<MisafirAyarlari>(builder.Configuration.GetSection(MisafirAyarlari.Bolum));
builder.Services.AddScoped<IEmailSender, MockEmailSender>();
builder.Services.AddScoped<IPersonelDizini, MockPersonelDizini>();
builder.Services.AddScoped<INetworkAccessProvider, SimulatedNetworkAccessProvider>();
builder.Services.AddScoped<IMisafirTalepServisi, MisafirTalepServisi>();
builder.Services.AddHostedService<MisafirTemizlikServisi>();

builder.Services.AddControllersWithViews(o =>
{
    // İlk girişte parola değişimini tüm uygulamada zorunlu kılar.
    o.Filters.Add<ParolaDegisimiZorunluFilter>();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Geliştirmede migration + seed otomatik uygulanır.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await VeriTohumlayici.SeedAsync(scope.ServiceProvider);
}

app.Run();
