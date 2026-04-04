using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuickShift.Data;
using QuickShift.DTOs;
using QuickShift.Models;
using QuickShift.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QuickShift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MasterDbContext _masterContext;
        private readonly IConfiguration _configuration;
        private readonly ITenantService _tenantService;

        public AuthController(MasterDbContext masterContext, IConfiguration configuration, ITenantService tenantService)
        {
            _masterContext = masterContext;
            _configuration = configuration;
            _tenantService = tenantService;
        }

        [HttpGet("login/{tenantId}")]
        public IActionResult Login(int tenantId)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback", new { tenantId }),
                Items = { { "tenantId", tenantId.ToString() } }
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] int tenantId)
        {
            var tenant = await _masterContext.Tenants.FindAsync(tenantId);
            if (tenant == null) return BadRequest("El Tenant no existe en la base maestra.");

            using var tenantContext = await _tenantService.GetContextAsync(tenantId);

            await tenantContext.Database.EnsureCreatedAsync();

            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded)
                return Unauthorized("Error de autenticación con Google.");

            var claimsIdentity = authenticateResult.Principal.Identity as ClaimsIdentity;
            var email = claimsIdentity?.FindFirst(ClaimTypes.Email)?.Value;
            var name = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email)) return BadRequest("No se pudo obtener el email de Google.");

            var user = await tenantContext.Users.SingleOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Name = name ?? "Usuario",
                    Role = "User",
                    CreatedAt = DateTime.UtcNow
                };
                tenantContext.Users.Add(user);
                await tenantContext.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user, tenantId);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new
            {
                token,
                user.Email,
                user.Name,
                user.Role,
                tenantId
            });
        }

        private string GenerateJwtToken(User user, int tenantId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("tenantId", tenantId.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLoginSpa([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Token);

                var tenant = await _masterContext.Tenants
                .FirstOrDefaultAsync(t => t.Name.ToLower() == request.TenantName.ToLower());

                if (tenant == null) return NotFound("La organización no existe.");

                int tenantIdNum = tenant.Id;

                var hasPermission = await _masterContext.UserTenants
                    .AnyAsync(ut => ut.UserEmail == payload.Email && ut.TenantId == tenantIdNum);

                using var tenantContext = await _tenantService.GetContextAsync(tenantIdNum);

                bool isFirstUser = !await tenantContext.Users.AnyAsync();

                if (!hasPermission && !isFirstUser)
                {
                    return Unauthorized(new { message = "No tienes acceso a esta organización. Solicita una invitación al administrador." });
                }

                var user = await tenantContext.Users.SingleOrDefaultAsync(u => u.Email == payload.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Email = payload.Email,
                        Name = payload.Name ?? "Usuario Nuevo",
                        Role = isFirstUser ? "Admin" : "User",
                        CreatedAt = DateTime.UtcNow
                    };
                    tenantContext.Users.Add(user);
                    await tenantContext.SaveChangesAsync();

                    if (!hasPermission)
                    {
                        _masterContext.UserTenants.Add(new UserTenant
                        {
                            UserEmail = payload.Email,
                            TenantId = tenantIdNum
                        });
                        await _masterContext.SaveChangesAsync();
                    }
                }

                var jwtToken = GenerateJwtToken(user, tenantIdNum);

                return Ok(new
                {
                    jwt = jwtToken,
                    user.Email,
                    user.Name,
                    user.Role,
                    tenantId = tenantIdNum
                });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized("Token de Google inválido.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno", details = ex.Message });
            }
        }

        [HttpPost("invite")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> InviteUser([FromBody] InviteRequest request)
        {
            var alreadyExists = await _masterContext.UserTenants
                .AnyAsync(ut => ut.UserEmail == request.Email && ut.TenantId == request.TenantId);

            if (alreadyExists) return BadRequest("El usuario ya tiene acceso a esta organización.");

            var newUserTenant = new UserTenant
            {
                UserEmail = request.Email.ToLower().Trim(),
                TenantId = request.TenantId
            };

            _masterContext.UserTenants.Add(newUserTenant);
            await _masterContext.SaveChangesAsync();

            return Ok(new { message = "Usuario invitado exitosamente." });
        }
        public class InviteRequest
        {
            public required string Email { get; set; }
            public required int TenantId { get; set; }
        }
    }
}