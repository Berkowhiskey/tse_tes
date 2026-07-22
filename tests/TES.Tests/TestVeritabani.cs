using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TES.Infrastructure.Data;
using TES.Infrastructure.Identity;

namespace TES.Tests;

/// <summary>
/// Servis testleri için SQLite in-memory veritabanı.
/// Bağlantı açık tutulduğu sürece veritabanı yaşar; Dispose ile yok olur.
/// </summary>
public sealed class TestVeritabani : IDisposable
{
    private readonly SqliteConnection _baglanti;

    public TesDbContext Db { get; }

    public TestVeritabani()
    {
        _baglanti = new SqliteConnection("DataSource=:memory:");
        _baglanti.Open();

        var options = new DbContextOptionsBuilder<TesDbContext>()
            .UseSqlite(_baglanti)
            .Options;

        Db = new TesDbContext(options);
        Db.Database.EnsureCreated();
    }

    /// <summary>FK bütünlüğü için test kullanıcısı ekler ve Id'sini döner.</summary>
    public string KullaniciEkle(string kullaniciAdi, string adSoyad)
    {
        var kullanici = new Kullanici
        {
            UserName = kullaniciAdi,
            AdSoyad = adSoyad
        };
        Db.Users.Add(kullanici);
        Db.SaveChanges();
        return kullanici.Id;
    }

    public void Dispose()
    {
        Db.Dispose();
        _baglanti.Dispose();
    }
}
