using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace CoreLayer.Entities.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public int? AddressId { get; set; }
        public Address? Address { get; set; }

        // Properties for OTP verification
        public string? VerificationCode { get; set; }
        public DateTime? CodeExpiresAt { get; set; }

        // FCM Device Token for push notifications
        public string? FCMToken { get; set; }
    }
}
