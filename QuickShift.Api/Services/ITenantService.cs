using QuickShift.Data;
using QuickShift.Models;

namespace QuickShift.Services
{
    public interface ITenantService
    {
        Task<AppDbContext> GetContextAsync(int tenantId);
        Task ProvisionTenantAsync(Tenant tenant);
    }
}