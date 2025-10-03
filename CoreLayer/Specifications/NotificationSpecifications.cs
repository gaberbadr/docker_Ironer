using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Entities.Orders;

namespace CoreLayer.Specifications
{
    public static class NotificationSpecifications
    {
        public class GetUserNotificationsSpec : BaseSpecifications<Notification, int>
        {
            public GetUserNotificationsSpec(string userId, int pageIndex, int pageSize) : base(n => n.ReceiverId == userId)
            {
                OrderByDescending = n => n.CreateTime;
                applyPagnation((pageIndex - 1) * pageSize, pageSize);
            }
        }

        public class GetUserNotificationsCountSpec : BaseSpecifications<Notification, int>
        {
            public GetUserNotificationsCountSpec(string userId) : base(n => n.ReceiverId == userId) { }
        }

        public class GetByUserIdSpec : BaseSpecifications<Notification, int>
        {
            public GetByUserIdSpec(string userId) : base(n => n.ReceiverId == userId || n.SenderId == userId) { }
        }

        public class GetUserMessagesSpec : BaseSpecifications<Notification, int>
        {
            public GetUserMessagesSpec(string userId, int pageIndex, int pageSize)
                : base(n => n.ReceiverId == userId || n.SenderId == userId)
            {
                OrderByDescending = n => n.CreateTime;
                applyPagnation((pageIndex - 1) * pageSize, pageSize);
            }
        }

        public class GetUserMessagesCountSpec : BaseSpecifications<Notification, int>
        {
            public GetUserMessagesCountSpec(string userId) : base(n => n.ReceiverId == userId || n.SenderId == userId) { }
        }

        public class GetUserUnreadNotificationsCountSpec : BaseSpecifications<Notification, int>
        {
            public GetUserUnreadNotificationsCountSpec(string userId) : base(n => n.ReceiverId == userId && !n.IsRead)
            {
            }
        }
    }
}
