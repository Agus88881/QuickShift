using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickShift.Data;
using QuickShift.DTOs;
using QuickShift.Models;

namespace QuickShift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly MasterDbContext _masterContext;

        public UserController(MasterDbContext masterContext)
        {
            _masterContext = masterContext;
        }

        private async Task<AppDbContext> GetTenantContextAsync(int tenantId)
        {
            var tenant = await _masterContext.Tenants.FindAsync(tenantId);
            if (tenant == null) throw new Exception("Tenant not found");

            var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={tenant.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True";
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            var context = new AppDbContext(optionsBuilder.Options);

            // Crea las tablas si no existen
            await context.Database.MigrateAsync();

            return context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser(
            [FromHeader(Name = "X-Tenant-Id")] int tenantId,
            [FromBody] CreateUserDto dto)
        {
            using var tenantContext = await GetTenantContextAsync(tenantId);

            var exists = await tenantContext.Users
                .AnyAsync(u => u.Email == dto.Email);

            if (exists)
                return BadRequest("A user with that email already exists.");

            var user = new User
            {
                Email = dto.Email,
                Name = dto.Name,
                Role = dto.Role,
                CreatedAt = DateTime.UtcNow
            };

            tenantContext.Users.Add(user);
            await tenantContext.SaveChangesAsync();

            return Ok(new
            {
                message = "User registered successfully!",
                userId = user.Id
            });
        }
    }
}