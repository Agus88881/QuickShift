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
    public class UserController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public UserController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser(
            [FromHeader(Name = "X-Tenant-Id")] int tenantId,
            [FromBody] CreateUserDto dto)
        {
            using var tenantContext = await _tenantService.GetContextAsync(tenantId);

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