using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CoreLayer.Service_contract
{
    public interface IUserService
    {
        Task<IActionResult> CreateOrderAsync(string userId, CreateOrderDto dto);
        Task<IActionResult> GetOrderStatusAsync(string userId, int orderId);
        Task<IActionResult> GetUserActiveOrdersAsync(string userId, int pageIndex, int pageSize);
        Task<IActionResult> GetOrderHistoryAsync(string userId, int pageIndex, int pageSize);
        Task<IActionResult> GetUserNotificationsAsync(string userId, int pageIndex, int pageSize);
        Task<IActionResult> GetUnreadNotificationsCountAsync(string userId);
        Task<IActionResult> CancelOrderAsync(string userId, int orderId);
        Task<IActionResult> UpdatePhoneAsync(string currentUserId, UpdatePhoneDto dto);
        Task<IActionResult> GetAllProductsAsync(int pageIndex, int pageSize);
        Task<IActionResult> GetAllTypesOfServiceAsync(int pageIndex, int pageSize);
        Task<IActionResult> GetAllDeliveryTypesAsync(int pageIndex, int pageSize);
        Task<IActionResult> GetUserAddressAsync(string userId);
        Task<IActionResult> UpdateUserAddressAsync(string userId, UpdateAddressDto dto);
        Task<IActionResult> UpdateFCMTokenAsync(string userId, UpdateFCMTokenDto dto);
    }

}
