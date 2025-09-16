using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Entities.Orders;
using static CoreLayer.Entities.Enum.Enums;

namespace CoreLayer.Specifications
{
    public static class OrderSpecifications
    {
        public class GetOrderByIdAndUserSpec : BaseSpecifications<Order, int>
        {
            public GetOrderByIdAndUserSpec(int orderId, string userId) : base(o => o.Id == orderId && o.UserId == userId)
            {
                Includes.Add(o => o.Address);
                Includes.Add(o => o.Coupon);
                Includes.Add(o => o.DeliveryType);
            }
        }

        public class GetUserOrderHistorySpec : BaseSpecifications<Order, int>
        {
            public GetUserOrderHistorySpec(string userId, int pageIndex, int pageSize) : base(o => o.UserId == userId)
            {
                Includes.Add(o => o.Address);
                Includes.Add(o => o.Coupon);
                Includes.Add(o => o.DeliveryType);
                OrderByDescending = o => o.CreateTime;
                applyPagnation((pageIndex - 1) * pageSize, pageSize);
            }
        }

        public class GetUserOrderCountSpec : BaseSpecifications<Order, int>
        {
            public GetUserOrderCountSpec(string userId) : base(o => o.UserId == userId) { }
        }

        public class GetByUserIdSpec : BaseSpecifications<Order, int>
        {
            public GetByUserIdSpec(string userId) : base(o => o.UserId == userId) { }
        }

        public class GetOrderByIdSpec : BaseSpecifications<Order, int>
        {
            public GetOrderByIdSpec(int orderId) : base(o => o.Id == orderId)
            {
                Includes.Add(o => o.Address);
                Includes.Add(o => o.Coupon);
                Includes.Add(o => o.DeliveryType);
            }
        }

        public class GetOrdersByStatusSpec : BaseSpecifications<Order, int>
        {
            public GetOrdersByStatusSpec(OrderStatus status, int pageIndex, int pageSize) : base(o => o.Status == status)
            {
                Includes.Add(o => o.Address);
                Includes.Add(o => o.Coupon);
                Includes.Add(o => o.DeliveryType);
                OrderByDescending = o => o.CreateTime;
                applyPagnation((pageIndex - 1) * pageSize, pageSize);
            }
        }

        public class GetOrdersByStatusCountSpec : BaseSpecifications<Order, int>
        {
            public GetOrdersByStatusCountSpec(OrderStatus status) : base(o => o.Status == status) { }
        }

        public class GetActiveOrdersSpec : BaseSpecifications<Order, int>
        {
            public GetActiveOrdersSpec(OrderStatus[] statuses, int pageIndex, int pageSize)
                : base(o => statuses.Contains(o.Status))
            {
                Includes.Add(o => o.Address);
                Includes.Add(o => o.Coupon);
                Includes.Add(o => o.DeliveryType);
                OrderByDescending = o => o.CreateTime;
                applyPagnation((pageIndex - 1) * pageSize, pageSize);
            }
        }

        public class GetActiveOrdersCountSpec : BaseSpecifications<Order, int>
        {
            public GetActiveOrdersCountSpec(OrderStatus[] statuses) : base(o => statuses.Contains(o.Status)) { }
        }

        public class GetAllOrdersHistorySpec : BaseSpecifications<Order, int>
        {
            public GetAllOrdersHistorySpec(int pageIndex, int pageSize)
            {
                Includes.Add(o => o.Address);
                Includes.Add(o => o.Coupon);
                Includes.Add(o => o.DeliveryType);
                OrderByDescending = o => o.CreateTime;
                applyPagnation((pageIndex - 1) * pageSize, pageSize);
            }
        }

        public class GetAllOrdersCountSpec : BaseSpecifications<Order, int>
        {
            public GetAllOrdersCountSpec() { }
        }

        public class GetUserActiveOrdersSpec : BaseSpecifications<Order, int>
        {
            public GetUserActiveOrdersSpec(string userId, int pageIndex, int pageSize)
                : base(o => o.UserId == userId && o.Status != OrderStatus.Paid && o.Status != OrderStatus.Cancelled)
            {
                Includes.Add(o => o.Address);
                Includes.Add(o => o.Coupon);
                Includes.Add(o => o.DeliveryType);
                OrderByDescending = o => o.CreateTime;
                applyPagnation((pageIndex - 1) * pageSize, pageSize);
            }
        }
        public class GetUserActiveOrdersCountSpec : BaseSpecifications<Order, int>
        {
            public GetUserActiveOrdersCountSpec(string userId)
                : base(o => o.UserId == userId && o.Status != OrderStatus.Paid && o.Status != OrderStatus.Cancelled) { }
        }


        public class GetAllPendingOrdersVipFirstSpec : BaseSpecifications<Order, int>
        {
            public GetAllPendingOrdersVipFirstSpec(int pageIndex, int pageSize) : base(o => o.Status == OrderStatus.Pending)
            {
                Includes.Add(o => o.Address);
                Includes.Add(o => o.Coupon);
                Includes.Add(o => o.DeliveryType);
                Includes.Add(o => o.User); // Need to include User to check roles
                OrderByDescending = o => o.CreateTime;
                applyPagnation((pageIndex - 1) * pageSize, pageSize);
            }
        }
    }

    public static class ProductSpecifications
    {
        public class GetAllProductsSpec : BaseSpecifications<Product, int>
        {
            public GetAllProductsSpec(int pageIndex, int pageSize)
            {
                OrderBy = p => p.Name;
                applyPagnation((pageIndex - 1) * pageSize, pageSize);
            }
        }

        public class GetProductsCountSpec : BaseSpecifications<Product, int>
        {
            public GetProductsCountSpec() { }
        }
    }

    public static class ServiceSpecifications
    {
        public class GetAllServicesSpec : BaseSpecifications<TypeOfService, int>
        {
            public GetAllServicesSpec(int pageIndex, int pageSize)
            {
                OrderBy = s => s.Name;
                applyPagnation((pageIndex - 1) * pageSize, pageSize);
            }
        }

        public class GetServicesCountSpec : BaseSpecifications<TypeOfService, int>
        {
            public GetServicesCountSpec() { }
        }
    }


    public static class ItemOrderSpecifications
    {
        public class GetByOrderIdSpec : BaseSpecifications<ItemOrder, int>
        {
            public GetByOrderIdSpec(int orderId) : base(i => i.OrderId == orderId) { }
        }
    }

    public static class OrderProductSpecifications
    {
        public class GetByItemOrderIdSpec : BaseSpecifications<OrderProduct, int>
        {
            public GetByItemOrderIdSpec(int itemOrderId) : base(p => p.ItemOrderId == itemOrderId) { }
        }
    }

    public static class OrderServiceSpecifications
    {
        public class GetByItemOrderIdSpec : BaseSpecifications<OrderService, int>
        {
            public GetByItemOrderIdSpec(int itemOrderId) : base(s => s.ItemOrderId == itemOrderId) { }
        }
    }


    public static class DeliveryTypeSpecifications
    {
        public class GetAllDeliveryTypesSpec : BaseSpecifications<DeliveryType, int>
        {
            public GetAllDeliveryTypesSpec(int pageIndex, int pageSize)
            {
                OrderBy = d => d.Name;
                applyPagnation((pageIndex - 1) * pageSize, pageSize);
            }
        }

        public class GetDeliveryTypesCountSpec : BaseSpecifications<DeliveryType, int>
        {
            public GetDeliveryTypesCountSpec() { }
        }
    }



}
