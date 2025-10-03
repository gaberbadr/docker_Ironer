using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLayer.Entities.Orders
{
    public class Coupon : BaseEntity<int>
    {
        public string Name { get; set; } = "";
        public decimal Rate { get; set; } // percentage or fixed amount
        public bool IsPercentage { get; set; } = true; // true for percentage, false for fixed amount
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }
    }
}
