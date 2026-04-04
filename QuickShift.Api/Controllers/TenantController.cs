using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickShift.Data;
using QuickShift.DTOs;
using QuickShift.Models;
using QuickShift.Services;

namespace QuickShift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantController : ControllerBase
    {
        private readonly MasterDbContext _masterContext;
        private readonly ITenantService _tenantService;

        public TenantController(MasterDbContext masterContext, ITenantService tenantService)
        {
            _masterContext = masterContext;
            _tenantService = tenantService;
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

            await _tenantService.ProvisionTenantAsync(tenant);

            return Ok(new
            {
                message = "Company registered and database provisioned successfully!",
                tenantId = tenant.Id,
                databaseName = tenant.DatabaseName
            });
        }
    }
}