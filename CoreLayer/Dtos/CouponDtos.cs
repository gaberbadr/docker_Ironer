using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreLayer.Dtos
{
    public class CouponDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        [JsonPropertyName("isPercentage")]
        public bool IsPercentage { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime? ExpiresAt { get; set; }
    }

    public class CreateCouponDto
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Rate must be greater than 0")]
        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        [JsonPropertyName("isPercentage")]
        public bool IsPercentage { get; set; } = true;

        [JsonPropertyName("expiresAt")]
        public DateTime? ExpiresAt { get; set; }
    }

}
