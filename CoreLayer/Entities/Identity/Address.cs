using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Entities.Orders;

namespace CoreLayer.Entities.Identity
{
    public class Address : BaseEntity<int>
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string Government { get; set; } = "";

        // Navigation property
        public ApplicationUser User { get; set; }
    }
}
