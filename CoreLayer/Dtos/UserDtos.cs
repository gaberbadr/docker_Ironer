using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreLayer.Dtos
{
    public class UpdatePhoneDto
    {
        [Required]
        [Phone]
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; } = "";
    }

    public class UserDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = "";

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = "";

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("emailConfirmed")]
        public bool EmailConfirmed { get; set; }

        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; } = new List<string>();

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    public class ChangeUserRoleDto
    {
        [Required]
        [JsonPropertyName("role")]
        public string Role { get; set; } = ""; // "Vip", "Blacklist"
    }

    public class AddAdminAssistantDto
    {
        [Required]
        [Phone]
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; } = "";
    }
}
