using System.Security.Claims;
using CoreLayer.Dtos;
using CoreLayer.Entities.Identity;
using CoreLayer.Helper.Pagination;
using CoreLayer.Service_contract;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ironer.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize(Policy = "NotBlacklisted")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _cfg;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(IUserService userService, UserManager<ApplicationUser> userManager, IConfiguration cfg)
        {
            _userService = userService;
            _cfg = cfg;
            _userManager = userManager;
        }

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var userId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.CreateOrderAsync(userId, dto);
        }

        [HttpGet("orders/{id}/status")]
        public async Task<IActionResult> GetOrderStatus(int id)
        {
            var userId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.GetOrderStatusAsync(userId, id);
        }

        [ProducesResponseType(typeof(PaginationResponse<OrderDto>), StatusCodes.Status200OK)]
        [HttpGet("orders/active")]
        public async Task<IActionResult> GetActiveOrders([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.GetUserActiveOrdersAsync(userId, pageIndex, pageSize);
        }

        [ProducesResponseType(typeof(PaginationResponse<OrderDto>), StatusCodes.Status200OK)]
        [HttpGet("orders/history")]
        public async Task<IActionResult> GetOrderHistory([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.GetOrderHistoryAsync(userId, pageIndex, pageSize);
        }

        [ProducesResponseType(typeof(PaginationResponse<NotificationDto>), StatusCodes.Status200OK)]
        [HttpGet("notifications")]
        public async Task<IActionResult> GetUserNotifications([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var currentUserId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.GetUserNotificationsAsync(currentUserId, pageIndex, pageSize);
        }

        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [HttpGet("notifications/unread-count")]
        public async Task<IActionResult> GetUnreadNotificationsCount()
        {
            var currentUserId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.GetUnreadNotificationsCountAsync(currentUserId);
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpDelete("orders/{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.CancelOrderAsync(userId, id);
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPut("userPhone")]
        public async Task<IActionResult> UpdatePhone([FromBody] UpdatePhoneDto dto)
        {
            var currentUserId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.UpdatePhoneAsync(currentUserId, dto);
        }

        [ProducesResponseType(typeof(PaginationResponse<ProductDto>), StatusCodes.Status200OK)]
        [HttpGet("products")]
        public async Task<IActionResult> GetAllProducts([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50)
        {
            return await _userService.GetAllProductsAsync(pageIndex, pageSize);
        }

        [ProducesResponseType(typeof(PaginationResponse<TypeOfServiceDto>), StatusCodes.Status200OK)]
        [HttpGet("typesOfService")]
        public async Task<IActionResult> GetAllTypesOfService([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50)
        {
            return await _userService.GetAllTypesOfServiceAsync(pageIndex, pageSize);
        }

        [ProducesResponseType(typeof(PaginationResponse<DeliveryTypeDto>), StatusCodes.Status200OK)]
        [HttpGet("DeliveryType")]
        public async Task<IActionResult> GetAllDeliveryType([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50)
        {
            return await _userService.GetAllDeliveryTypesAsync(pageIndex, pageSize);
        }

        [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
        [HttpGet("{userId}/address")]
        [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
        [HttpGet("address")]
        public async Task<IActionResult> GetMyAddress()
        {
            var currentUserId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.GetUserAddressAsync(currentUserId);
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPut("address")]
        public async Task<IActionResult> UpdateMyAddress([FromBody] UpdateAddressDto dto)
        {
            var currentUserId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.UpdateUserAddressAsync(currentUserId, dto);
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPut("firstName")]
        public async Task<IActionResult> UpdateFirstName([FromBody] UpdateNameDto dto)
        {
            var currentUserId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.UpdateFirstNameAsync(currentUserId, dto);
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPut("lastName")]
        public async Task<IActionResult> UpdateLastName([FromBody] UpdateLastNameDto dto)
        {
            var currentUserId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.UpdateLastNameAsync(currentUserId, dto);
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordDto dto)
        {
            var currentUserId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.SetPasswordAsync(currentUserId, dto);
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPut("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            var currentUserId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { Message = "User not authenticated" });

            return await _userService.UpdatePasswordAsync(currentUserId, dto);
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPut("fcm-token")]
        public async Task<IActionResult> UpdateFCMToken([FromBody] UpdateFCMTokenDto dto)
        {
            var currentUserId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { Message = "User not authenticated" });

            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Invalid FCM token",
                    Errors = ModelState.ToDictionary(
                        x => x.Key,
                        x => x.Value?.Errors?.Select(e => e.ErrorMessage).ToArray() ?? new string[0]
                    )
                });
            }

            return await _userService.UpdateFCMTokenAsync(currentUserId, dto);
        }

    }
}
