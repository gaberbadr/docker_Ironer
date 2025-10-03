using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLayer.Entities.Orders
{
    public class ItemOrder : BaseEntity<int>
    {
        public int OrderId { get; set; }
        public string Notes { get; set; } = "";

        // Navigation properties
        public virtual Order Order { get; set; }
        public virtual ICollection<OrderProduct> Products { get; set; } = new List<OrderProduct>();
        public virtual ICollection<OrderService> Services { get; set; } = new List<OrderService>();
    }
}
