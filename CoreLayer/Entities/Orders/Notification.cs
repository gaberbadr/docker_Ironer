using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Entities.Identity;
using static CoreLayer.Entities.Enum.Enums;

namespace CoreLayer.Entities.Orders
{
    public class Notification : BaseEntity<int>
    {
        public string SenderId { get; set; } = ""; // Admin or Admin Assistant
        public string ReceiverId { get; set; } = ""; // User
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? MediaUrl { get; set; }
        public NotificationType Type { get; set; } = NotificationType.Message;
        public bool IsRead { get; set; } = false;

        // Navigation properties
        public virtual ApplicationUser Sender { get; set; }
        public virtual ApplicationUser Receiver { get; set; }
    }
}
