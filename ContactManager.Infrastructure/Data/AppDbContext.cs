using ContactManager.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ContactManager.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Contato> Contatos { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Contato>(entity =>
        {
            entity.ToTable("Contatos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Sexo).IsRequired().HasMaxLength(10);
            entity.Property(e => e.DataNascimento).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Ignore(e => e.Idade);
            
            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey("UserId")
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
