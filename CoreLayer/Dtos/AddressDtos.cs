using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreLayer.Dtos
{
    public class AddressDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("street")]
        public string Street { get; set; } = "";

        [JsonPropertyName("city")]
        public string City { get; set; } = "";

        [JsonPropertyName("government")]
        public string Government { get; set; } = "";
    }
}
