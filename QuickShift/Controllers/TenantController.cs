using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickShift.Data;
using QuickShift.DTOs;
using QuickShift.Models;

namespace QuickShift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantController : ControllerBase
    {
        private readonly MasterDbContext _masterContext;

        public TenantController(MasterDbContext masterContext)
        {
            _masterContext = masterContext;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterTenant([FromBody] CreateTenantDto dto)
        {
            var exists = await _masterContext.Tenants
                .AnyAsync(t => t.Name == dto.Name);

            if (exists)
                return BadRequest("A company with that name already exists.");

            var tenant = new Tenant
            {
                Name = dto.Name,
                DatabaseName = $"QuickShift_{dto.Name.Replace(" ", "_")}",
                CreatedAt = DateTime.UtcNow
            };

            _masterContext.Tenants.Add(tenant);
            await _masterContext.SaveChangesAsync();

            var tenantConnectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={tenant.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(tenantConnectionString);

            using (var tenantContext = new AppDbContext(optionsBuilder.Options))
            {
                await tenantContext.Database.MigrateAsync();
            }

            return Ok(new
            {
                message = "Company registered successfully!",
                tenantId = tenant.Id,
                databaseName = tenant.DatabaseName
            });
        }
    }
}