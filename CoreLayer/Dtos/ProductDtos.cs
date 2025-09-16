using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreLayer.Dtos
{
    public class ProductDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }

    public class CreateProductDto
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}
