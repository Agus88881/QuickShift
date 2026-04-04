using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using QuickShift.Data;
using QuickShift.Models;

namespace QuickShift.Services
{
    public class TenantService : ITenantService
    {
        private readonly MasterDbContext _masterContext;
        private readonly IConfiguration _configuration;

        public TenantService(MasterDbContext masterContext, IConfiguration configuration)
        {
            _masterContext = masterContext;
            _configuration = configuration;
        }

        public async Task<AppDbContext> GetContextAsync(int tenantId)
        {
            var tenant = await _masterContext.Tenants.FindAsync(tenantId)
                         ?? throw new Exception("Tenant no encontrado.");

            var context = CreateContext(tenant.DatabaseName);

            var databaseCreator = context.Database.GetService<IRelationalDatabaseCreator>();

            if (!await databaseCreator.ExistsAsync()) await databaseCreator.CreateAsync();
            try
            {
                await databaseCreator.CreateTablesAsync();
            }
            catch
            {
            }

            return context;
        }

        public async Task ProvisionTenantAsync(Tenant tenant)
        {
            using var context = CreateContext(tenant.DatabaseName);
            await context.Database.MigrateAsync();
        }

        private AppDbContext CreateContext(string dbName)
        {
            var template = _configuration.GetConnectionString("TenantConnectionTemplate")!;
            var connectionString = template.Replace("{TenantDb}", dbName);

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}