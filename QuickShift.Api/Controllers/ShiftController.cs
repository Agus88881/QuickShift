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

        [HttpGet("status")]
        public async Task<IActionResult> GetCurrentStatus()
        {
            var tenantIdClaim = User.FindFirst("tenantId")?.Value;
            var userEmailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userEmailClaim))
                return Unauthorized();

            int tenantId = int.Parse(tenantIdClaim);

            using var tenantContext = await _tenantService.GetContextAsync(tenantId);

            var user = await tenantContext.Users.FirstOrDefaultAsync(u => u.Email == userEmailClaim);
            if (user == null) return NotFound("Usuario no registrado en este tenant.");

            var activeShift = await tenantContext.Shifts
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.ClockOut == null);

            return Ok(new
            {
                isClockedIn = activeShift != null,
                startTime = activeShift?.ClockIn,
                shiftId = activeShift?.Id
            });
        }

        [HttpPost("clockin")]
        public async Task<IActionResult> ClockIn()
        {
            var tenantIdClaim = User.FindFirst("tenantId")?.Value;
            var userEmailClaim = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userEmailClaim))
                return Unauthorized("Claims inválidos.");

            int tenantId = int.Parse(tenantIdClaim);

            using var tenantContext = await _tenantService.GetContextAsync(tenantId);

            var user = await tenantContext.Users.FirstOrDefaultAsync(u => u.Email == userEmailClaim);

            if (user == null) return NotFound("Usuario no existe en este Tenant.");

            int localUserId = user.Id;

            var alreadyClockedIn = await tenantContext.Shifts
                .AnyAsync(s => s.UserId == localUserId && s.ClockOut == null);

            if (alreadyClockedIn) return BadRequest("Ya tenés un turno abierto.");

            var shift = new Shift
            {
                UserId = localUserId,
                ClockIn = DateTime.UtcNow
            };

            tenantContext.Shifts.Add(shift);
            await tenantContext.SaveChangesAsync();

            return Ok(new { message = "Entrada registrada", localId = localUserId });
        }

        [HttpPost("clockout")]
        public async Task<IActionResult> ClockOut()
        {
            var tenantIdClaim = User.FindFirst("tenantId")?.Value;
            var userEmailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userEmailClaim))
                return Unauthorized("Invalid token claims.");

            int tenantId = int.Parse(tenantIdClaim);

            using var tenantContext = await _tenantService.GetContextAsync(tenantId);

            var user = await tenantContext.Users.FirstOrDefaultAsync(u => u.Email == userEmailClaim);
            if (user == null)
                return NotFound("User not found in this tenant.");

            var openShift = await tenantContext.Shifts
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.ClockOut == null);

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