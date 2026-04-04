using System.Text.Json.Serialization;

namespace QuickShift.DTOs
{
    public class GoogleLoginRequest
    {
        [JsonPropertyName("token")]
        public required string Token { get; set; }
        [JsonPropertyName("tenantName")]
        public required string TenantName { get; set; }
    }
}   