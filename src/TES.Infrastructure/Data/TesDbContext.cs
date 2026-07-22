using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TES.Domain.Entities;
using TES.Infrastructure.Identity;

namespace TES.Infrastructure.Data;

public class TesDbContext(DbContextOptions<TesDbContext> options)
    : IdentityDbContext<Kullanici>(options)
{
    public DbSet<DenetimKaydi> DenetimKayitlari => Set<DenetimKaydi>();

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
    }
}
