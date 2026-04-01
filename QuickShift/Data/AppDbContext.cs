using Microsoft.EntityFrameworkCore;
using QuickShift.Models;

namespace QuickShift.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Shift> Shifts { get; set; }
    }
}
