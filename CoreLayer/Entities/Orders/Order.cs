using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Entities.Identity;
using static CoreLayer.Entities.Enum.Enums;

namespace CoreLayer.Entities.Orders
{
    public class Order : BaseEntity<int>
    {
        public string UserId { get; set; } = "";
        public int? CouponId { get; set; }
        public int DeliveryTypeId { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal DeliveryPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Phone { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ApplicationUser User { get; set; }
        public virtual Coupon? Coupon { get; set; }
        public virtual DeliveryType DeliveryType { get; set; }
        public virtual ICollection<ItemOrder> Items { get; set; } = new List<ItemOrder>();

        // own order address
        public OrderAddress Address { get; set; }
    }

}
