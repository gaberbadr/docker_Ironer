using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLayer.Entities.Orders
{
    public class DeliveryType : BaseEntity<int>
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}
