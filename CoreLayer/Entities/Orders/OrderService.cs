using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLayer.Entities.Orders
{
    public class OrderService : BaseEntity<int>
    {
        public int ItemOrderId { get; set; }
        public int TypeOfServiceId { get; set; }

        // Navigation properties
        public virtual ItemOrder ItemOrder { get; set; }
        public virtual TypeOfService TypeOfService { get; set; }
    }
}
