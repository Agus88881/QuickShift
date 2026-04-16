using Microsoft.EntityFrameworkCore;
using QuickShift.Models;

public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options)
        : base(options) { }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<UserTenant> UserTenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserTenant>()
            .HasKey(ut => new { ut.UserEmail, ut.TenantId });

        base.OnModelCreating(modelBuilder);
    }
}