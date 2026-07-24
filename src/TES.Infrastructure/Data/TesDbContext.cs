using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TES.Domain.Entities;
using TES.Infrastructure.Identity;
using TES.Infrastructure.Simulation;

namespace TES.Infrastructure.Data;

public class TesDbContext(DbContextOptions<TesDbContext> options)
    : IdentityDbContext<Kullanici>(options)
{
    public DbSet<DenetimKaydi> DenetimKayitlari => Set<DenetimKaydi>();
    public DbSet<Departman> Departmanlar => Set<Departman>();
    public DbSet<AmirProfil> AmirProfilleri => Set<AmirProfil>();
    public DbSet<StajyerProfil> StajyerProfilleri => Set<StajyerProfil>();
    public DbSet<YoklamaKaydi> YoklamaKayitlari => Set<YoklamaKaydi>();
    public DbSet<Proje> Projeler => Set<Proje>();
    public DbSet<Odev> Odevler => Set<Odev>();
    public DbSet<Gonderi> Gonderiler => Set<Gonderi>();
    public DbSet<Yorum> Yorumlar => Set<Yorum>();
    public DbSet<Begeni> Begeniler => Set<Begeni>();
    public DbSet<MisafirErisimTalebi> MisafirErisimTalepleri => Set<MisafirErisimTalebi>();
    public DbSet<GidenEposta> GidenEpostalar => Set<GidenEposta>();
    public DbSet<SimuleAgErisimi> SimuleAgErisimleri => Set<SimuleAgErisimi>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DenetimKaydi>(e =>
        {
            e.Property(x => x.Aktor).HasMaxLength(256);
            e.Property(x => x.Islem).HasMaxLength(128);
            e.Property(x => x.Detay).HasMaxLength(1024);
            e.HasIndex(x => x.Zaman);
        });

        builder.Entity<Departman>(e =>
        {
            e.Property(x => x.Ad).HasMaxLength(128);
            e.HasIndex(x => new { x.UstDepartmanId, x.Ad }).IsUnique();

            // Hiyerarşide yanlışlıkla zincirleme silme olmasın.
            e.HasOne(x => x.UstDepartman)
                .WithMany(x => x.AltDepartmanlar)
                .HasForeignKey(x => x.UstDepartmanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AmirProfil>(e =>
        {
            e.Property(x => x.KullaniciId).HasMaxLength(450);
            e.Property(x => x.Hakkimda).HasMaxLength(2000);
            e.HasIndex(x => x.KullaniciId).IsUnique();

            e.HasOne<Kullanici>()
                .WithOne()
                .HasForeignKey<AmirProfil>(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Departman)
                .WithMany()
                .HasForeignKey(x => x.DepartmanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StajyerProfil>(e =>
        {
            e.Property(x => x.KullaniciId).HasMaxLength(450);
            e.Property(x => x.KartNo).HasMaxLength(64);
            e.Property(x => x.Okul).HasMaxLength(256);
            e.Property(x => x.Bolum).HasMaxLength(256);
            e.HasIndex(x => x.KullaniciId).IsUnique();
            e.HasIndex(x => x.KartNo).IsUnique();

            e.HasOne<Kullanici>()
                .WithOne()
                .HasForeignKey<StajyerProfil>(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            // Tek amir; amir silinirse stajyer amir'siz kalır (eşleştirme admin'e düşer).
            // ClientSetNull: SQL Server'da AspNetUsers üzerinden çoklu cascade yolu
            // oluşmaması için null'lama istemci (EF) tarafında yapılır.
            e.HasOne(x => x.Amir)
                .WithMany(x => x.Stajyerler)
                .HasForeignKey(x => x.AmirId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            e.HasOne(x => x.Departman)
                .WithMany()
                .HasForeignKey(x => x.DepartmanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<YoklamaKaydi>(e =>
        {
            e.HasIndex(x => new { x.StajyerProfilId, x.GirisZamani });

            e.HasOne(x => x.StajyerProfil)
                .WithMany(x => x.YoklamaKayitlari)
                .HasForeignKey(x => x.StajyerProfilId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Proje>(e =>
        {
            e.Property(x => x.Ad).HasMaxLength(200);
            e.Property(x => x.Aciklama).HasMaxLength(2000);
            // Stajyer başına tek proje.
            e.HasIndex(x => x.StajyerProfilId).IsUnique();

            e.HasOne(x => x.StajyerProfil)
                .WithOne(x => x.Proje)
                .HasForeignKey<Proje>(x => x.StajyerProfilId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Odev>(e =>
        {
            e.Property(x => x.Baslik).HasMaxLength(200);
            e.Property(x => x.Aciklama).HasMaxLength(2000);
            e.HasIndex(x => x.StajyerProfilId);
            e.HasIndex(x => x.AmirProfilId);

            e.HasOne(x => x.StajyerProfil)
                .WithMany(x => x.Odevler)
                .HasForeignKey(x => x.StajyerProfilId)
                .OnDelete(DeleteBehavior.Cascade);

            // Amir tarafı Restrict: SQL Server'da AspNetUsers üzerinden çoklu cascade yolu engellenir.
            e.HasOne(x => x.AmirProfil)
                .WithMany()
                .HasForeignKey(x => x.AmirProfilId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Gonderi>(e =>
        {
            e.Property(x => x.YazarId).HasMaxLength(450);
            e.Property(x => x.Icerik).HasMaxLength(4000);
            e.Property(x => x.RedMesaji).HasMaxLength(1000);
            e.Property(x => x.ModeratorAdi).HasMaxLength(256);
            e.HasIndex(x => x.YazarId);
            e.HasIndex(x => x.ModerasyonDurumu);
        });

        builder.Entity<Yorum>(e =>
        {
            e.Property(x => x.YazarId).HasMaxLength(450);
            e.Property(x => x.Icerik).HasMaxLength(2000);
            e.HasIndex(x => x.GonderiId);

            e.HasOne(x => x.Gonderi)
                .WithMany(x => x.Yorumlar)
                .HasForeignKey(x => x.GonderiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Begeni>(e =>
        {
            e.Property(x => x.KullaniciId).HasMaxLength(450);
            // Bir kullanıcı bir gönderiyi bir kez beğenir.
            e.HasIndex(x => new { x.GonderiId, x.KullaniciId }).IsUnique();

            e.HasOne(x => x.Gonderi)
                .WithMany(x => x.Begeniler)
                .HasForeignKey(x => x.GonderiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MisafirErisimTalebi>(e =>
        {
            e.Property(x => x.AdSoyad).HasMaxLength(128);
            e.Property(x => x.Eposta).HasMaxLength(256);
            e.Property(x => x.SponsorEposta).HasMaxLength(256);
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.Property(x => x.VoucherHash).HasMaxLength(64);
            e.Property(x => x.KarariVeren).HasMaxLength(256);

            e.HasIndex(x => x.TakipKodu).IsUnique();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.SponsorEposta, x.OlusturmaZamani }); // rate-limit sorgusu
            e.HasIndex(x => x.Durum);

            e.HasOne(x => x.StajyerProfil)
                .WithMany()
                .HasForeignKey(x => x.StajyerProfilId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GidenEposta>(e =>
        {
            e.Property(x => x.Kime).HasMaxLength(256);
            e.Property(x => x.Konu).HasMaxLength(256);
            e.HasIndex(x => x.Zaman);
        });

        builder.Entity<SimuleAgErisimi>(e =>
        {
            e.HasIndex(x => x.MisafirErisimTalebiId).IsUnique();
        });
    }
}
