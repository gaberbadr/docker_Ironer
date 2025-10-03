using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLayer.Entities.Enum
{
    public class Enums
    {

        // User roles enum
        public static class UserRoles
        {
            public const string Admin = "Admin";
            public const string AdminAssistant = "AdminAssistant";
            public const string Vip = "Vip";
            public const string Blacklist = "Blacklist";
        }

        public enum NotificationType
        {
            Message,
            Image,
            Video,
            Application
        }

        public enum OrderStatus
        {
            Pending,
            Accepted,
            Processing,
            ReadyForPickup,
            OutForDelivery,
            Delivered,
            Paid,
            Cancelled
        }
    }
}
