using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickShift.Data;
using QuickShift.DTOs;
using QuickShift.Models;
using System.Runtime.InteropServices;

namespace QuickShift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShiftController : ControllerBase
    {
        private readonly MasterDbContext _masterContext;

        public ShiftController(MasterDbContext masterContext)
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
            await context.Database.MigrateAsync();
            return context;
        }

        [HttpPost("clockin")]
        public async Task<IActionResult> ClockIn([FromHeader(Name = "X-Tenant-Id")] int tenantId,[FromBody] ClockInDto dto)
        {
            using var tenantContext = await GetTenantContextAsync(tenantId);

            var user = await tenantContext.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound("User not found.");

            var openShift = await tenantContext.Shifts
                .AnyAsync(s => s.UserId == dto.UserId && s.ClockOut == null);

            if (openShift)
                return BadRequest("User already clocked in.");

            var shift = new Shift
            {
                UserId = dto.UserId,
                ClockIn = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            tenantContext.Shifts.Add(shift);
            await tenantContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Clocked in successfully!",
                shiftId = shift.Id,
                clockIn = shift.ClockIn
            });
        }

        [HttpPost("clockout")]
        public async Task<IActionResult> ClockOut([FromHeader(Name = "X-Tenant-Id")] int tenantId,[FromBody] ClockInDto dto)
        {
            using var tenantContext = await GetTenantContextAsync(tenantId);

            var openShift = await tenantContext.Shifts
                .FirstOrDefaultAsync(s => s.UserId == dto.UserId && s.ClockOut == null);

            if (openShift == null)
                return BadRequest("User is not clocked in.");

            openShift.ClockOut = DateTime.UtcNow;
            await tenantContext.SaveChangesAsync();

            var hoursWorked = (openShift.ClockOut.Value - openShift.ClockIn).TotalHours;

            return Ok(new
            {
                message = "Clocked out successfully!",
                shiftId = openShift.Id,
                clockIn = openShift.ClockIn,
                clockOut = openShift.ClockOut,
                hoursWorked = Math.Round(hoursWorked, 2)
            });
        }
    }
}