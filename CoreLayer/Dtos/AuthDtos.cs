using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreLayer.Dtos
{
    // DTO for sending verification code (used in send-verification-code endpoint)
    public class EmailDto
    {
        [Required, EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";
    }

    // DTO for sending verification code (alternative name - you can use either)
    public class SendVerificationCodeDto
    {
        [Required, EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";
    }

    // DTO for verifying the OTP code
    public class VerifyCodeDto
    {
        [Required, EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";
    }

    // DTO for token response
    public class TokenResponseDto
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("accessTokenExpiresAt")]
        public DateTime AccessTokenExpiresAt { get; set; }

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = "";

        [JsonPropertyName("refreshTokenExpiresAt")]
        public DateTime RefreshTokenExpiresAt { get; set; }
    }

    // DTO for refresh token requests
    public class RefreshRequestDto
    {
        [Required]
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = "";
    }


    // DTO for user profile response
    public class UserProfileDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("emailConfirmed")]
        public bool EmailConfirmed { get; set; }

        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; } = new List<string>();
    }

    // DTO for error responses
    public class ErrorResponseDto
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("errors")]
        public Dictionary<string, string[]>? Errors { get; set; }
    }

    // DTO for success responses
    public class SuccessResponseDto
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }
}
