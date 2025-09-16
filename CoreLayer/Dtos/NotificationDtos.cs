using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CoreLayer.Dtos
{
    public class SendNotificationDto
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Message { get; set; } = "";

        [Required]
        public string Type { get; set; } = "Message"; // Message, Image, Video ,Application

        public IFormFile? MediaFile { get; set; }
    }

    public class SendNotificationToAllDto
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Message { get; set; } = "";

        [Required]
        public string Type { get; set; } = "Message"; // Message, Image, Video ,Application

        public IFormFile? MediaFile { get; set; }
    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = "";
        public string SenderName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? MediaUrl { get; set; }
        public string Type { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateFCMTokenDto
    {
        [Required]
        public string FCMToken { get; set; } = "";
    }
}
