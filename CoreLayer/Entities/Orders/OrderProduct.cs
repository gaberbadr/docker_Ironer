using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLayer.Entities.Orders
{
    public class OrderProduct : BaseEntity<int>
    {
        public int ItemOrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Navigation properties
        public virtual ItemOrder ItemOrder { get; set; }
        public virtual Product Product { get; set; }
    }

}
