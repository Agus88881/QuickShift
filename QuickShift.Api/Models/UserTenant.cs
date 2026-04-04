namespace QuickShift.Models
{
    public class UserTenant
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public int TenantId { get; set; }

        public Tenant? Tenant { get; set; }
    }
}