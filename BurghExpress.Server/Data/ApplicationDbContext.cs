using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using BurghExpress.Server.Models;


namespace BurghExpress.Server.Data;


public class ApplicationDbContext : IdentityDbContext<User, Role, int>
{
  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

  public DbSet<Permission> Permissions { get; set; }

  public DbSet<UserPermission> UserPermissions { get; set; }

  public DbSet<RolePermission> RolePermissions { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<IdentityUserLogin<int>>(entity => 
        {
        entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });
        });
    modelBuilder.Entity<IdentityUserRole<int>>(entity => 
        {
        entity.HasKey(e => new { e.RoleId, e.UserId });
        });
    modelBuilder.Entity<IdentityUserToken<int>>(entity =>
        {
        entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });
        });
    modelBuilder.Entity<IdentityRoleClaim<int>>(entity =>
        {
        entity.HasKey(e => new { e.Id });
        });
    modelBuilder.Entity<User>(entity => 
        {
        entity.HasIndex(e => e.UserName).IsUnique();
        });

    modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

    modelBuilder.Entity<User>()
      .Property(u => u.UpdatedAt)
      .HasDefaultValueSql("CURRENT_TIMESTAMP")
      .ValueGeneratedOnAddOrUpdate();

    modelBuilder.Entity<Permission>()
      .Property(p => p.UpdatedAt)
      .HasDefaultValueSql("CURRENT_TIMESTAMP")
      .ValueGeneratedOnAddOrUpdate();

    modelBuilder.Entity<UserPermission>()
      .Property(up => up.UpdatedAt)
      .HasDefaultValueSql("CURRENT_TIMESTAMP")
      .ValueGeneratedOnAddOrUpdate();

    modelBuilder.Entity<Role>()
      .Property(r => r.UpdatedAt)
      .HasDefaultValueSql("CURRENT_TIMESTAMP")
      .ValueGeneratedOnAddOrUpdate();

    modelBuilder.Entity<RolePermission>()
      .Property(rp => rp.UpdatedAt)
      .HasDefaultValueSql("CURRENT_TIMESTAMP")
      .ValueGeneratedOnAddOrUpdate();


  }
}
