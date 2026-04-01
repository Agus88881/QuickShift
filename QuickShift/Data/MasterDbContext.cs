using Microsoft.EntityFrameworkCore;
using QuickShift.Models;

namespace QuickShift.Data
{
    public class MasterDbContext : DbContext
    {
        public MasterDbContext(DbContextOptions<MasterDbContext> options)
            : base(options) { }

        public DbSet<Tenant> Tenants { get; set; }
    }
}
