using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLayer.Dtos
{
    public class UpdateAddressDto
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string Government { get; set; } = "";
    }
}
