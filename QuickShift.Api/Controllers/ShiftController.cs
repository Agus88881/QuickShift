using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickShift.Models;
using QuickShift.Services;
using System.Security.Claims;

namespace QuickShift.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShiftController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public ShiftController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpPost("clockin")]
        public async Task<IActionResult> ClockIn()
        {
            var tenantIdClaim = User.FindFirst("tenantId")?.Value;
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Invalid token claims.");

            int tenantId = int.Parse(tenantIdClaim);
            int userId = int.Parse(userIdClaim);

            using var tenantContext = await _tenantService.GetContextAsync(tenantId);

            var user = await tenantContext.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found in this tenant.");

            var alreadyClockedIn = await tenantContext.Shifts
                .AnyAsync(s => s.UserId == userId && s.ClockOut == null);

            if (alreadyClockedIn)
                return BadRequest("User already clocked in.");

            var shift = new Shift
            {
                UserId = userId,
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
        public async Task<IActionResult> ClockOut()
        {
            var tenantIdClaim = User.FindFirst("tenantId")?.Value;
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Invalid token claims.");

            int tenantId = int.Parse(tenantIdClaim);
            int userId = int.Parse(userIdClaim);

            using var tenantContext = await _tenantService.GetContextAsync(tenantId);

            var openShift = await tenantContext.Shifts
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ClockOut == null);

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