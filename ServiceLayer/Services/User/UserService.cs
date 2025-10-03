using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using CoreLayer;
using CoreLayer.Dtos;
using CoreLayer.Entities.Identity;
using CoreLayer.Entities.Orders;
using CoreLayer.Helper.Pagination;
using CoreLayer.Service_contract;
using CoreLayer.Specifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using static CoreLayer.Entities.Enum.Enums;

namespace ServiceLayer.Services.User
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHost;
        private readonly IConfiguration _cfg;

        public UserService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHost,
            IConfiguration cfg)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHost = webHost;
            _cfg = cfg;
        }

        // ========== Create New Order ==========
        public async Task<IActionResult> CreateOrderAsync(string userId, CreateOrderDto dto)
        {
            if (dto == null)
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "Invalid order data" });

            // Validate delivery type exists
            var deliveryType = await _unitOfWork.Repository<DeliveryType, int>().GetAsync(dto.DeliveryTypeId);
            if (deliveryType == null)
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "Invalid delivery type" });

            // Validate coupon if provided (by name instead of ID)
            Coupon? coupon = null;
            if (!string.IsNullOrWhiteSpace(dto.CouponName))
            {
                var coupons = await _unitOfWork.Repository<Coupon, int>().GetAllAsync();
                coupon = coupons.FirstOrDefault(c => c.Name.ToLower() == dto.CouponName.ToLower());

                if (coupon == null)
                    return new BadRequestObjectResult(new ErrorResponseDto { Message = "Coupon not found" });

                if (!coupon.IsActive || (coupon.ExpiresAt.HasValue && coupon.ExpiresAt < DateTime.UtcNow))
                    return new BadRequestObjectResult(new ErrorResponseDto { Message = "Coupon is inactive or expired" });
            }

            // Calculate total price
            decimal itemsTotal = 0;
            foreach (var item in dto.Items)
            {
                foreach (var product in item.Products)
                {
                    var productEntity = await _unitOfWork.Repository<Product, int>().GetAsync(product.ProductId);
                    if (productEntity == null)
                        return new BadRequestObjectResult(new ErrorResponseDto { Message = $"Invalid product ID: {product.ProductId}" });

                    itemsTotal += productEntity.Price * product.Quantity;
                }

                foreach (var service in item.Services)
                {
                    var serviceEntity = await _unitOfWork.Repository<TypeOfService, int>().GetAsync(service.TypeOfServiceId);
                    if (serviceEntity == null)
                        return new BadRequestObjectResult(new ErrorResponseDto { Message = $"Invalid service ID: {service.TypeOfServiceId}" });

                    itemsTotal += serviceEntity.Price;
                }
            }

            decimal discount = coupon != null
                ? (coupon.IsPercentage ? (itemsTotal * coupon.Rate / 100) : coupon.Rate)
                : 0;

            decimal totalPrice = itemsTotal + deliveryType.Price - discount;

            // Create order
            var order = new Order
            {
                UserId = userId,
                CouponId = coupon?.Id, // Use coupon ID if found
                DeliveryTypeId = dto.DeliveryTypeId,
                Phone = dto.Phone,
                Status = OrderStatus.Pending,
                DeliveryPrice = deliveryType.Price,
                TotalPrice = totalPrice,
                Address = new OrderAddress
                {
                    Street = dto.Address.Street,
                    City = dto.Address.City,
                    Government = dto.Address.Government
                }
            };

            await _unitOfWork.Repository<Order, int>().AddAsync(order);

            // Create item orders (rest remains the same)
            foreach (var itemDto in dto.Items)
            {
                var itemOrder = new ItemOrder
                {
                    Order = order,
                    Notes = itemDto.Notes
                };

                await _unitOfWork.Repository<ItemOrder, int>().AddAsync(itemOrder);

                // Add products
                foreach (var productDto in itemDto.Products)
                {
                    var orderProduct = new OrderProduct
                    {
                        ItemOrder = itemOrder,
                        ProductId = productDto.ProductId,
                        Quantity = productDto.Quantity
                    };
                    await _unitOfWork.Repository<OrderProduct, int>().AddAsync(orderProduct);
                }

                // Add services
                foreach (var serviceDto in itemDto.Services)
                {
                    var orderService = new OrderService
                    {
                        ItemOrder = itemOrder,
                        TypeOfServiceId = serviceDto.TypeOfServiceId
                    };
                    await _unitOfWork.Repository<OrderService, int>().AddAsync(orderService);
                }
            }

            await _unitOfWork.CompleteAsync();

            return new OkObjectResult(new { OrderId = order.Id, Message = "Order created successfully" });
        }

        // ========== Get Order Status ==========
        public async Task<IActionResult> GetOrderStatusAsync(string userId, int orderId)
        {
            var spec = new OrderSpecifications.GetOrderByIdAndUserSpec(orderId, userId);
            var order = await _unitOfWork.Repository<Order, int>().GetWithSpecficationAsync(spec);

            if (order == null)
                return new NotFoundObjectResult(new ErrorResponseDto { Message = "Order not found" });

            if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "Cannot get status for completed or cancelled orders" });

            return new OkObjectResult(new
            {
                OrderId = order.Id,
                Status = order.Status.ToString(),
                UpdatedAt = order.UpdatedAt
            });
        }

        // ========== Get Order History ==========
        public async Task<IActionResult> GetOrderHistoryAsync(string userId, int pageIndex, int pageSize)
        {
            var spec = new OrderSpecifications.GetUserOrderHistorySpec(userId, pageIndex, pageSize);
            var orders = await _unitOfWork.Repository<Order, int>().GetAllWithSpecficationAsync(spec);
            var count = await _unitOfWork.Repository<Order, int>().GetCountAsync(new OrderSpecifications.GetUserOrderCountSpec(userId));

            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                Address = new OrderAddressDto
                {
                    Street = o.Address.Street,
                    City = o.Address.City,
                    Government = o.Address.Government
                },
                Coupon = o.Coupon != null ? new CouponDto
                {
                    Id = o.Coupon.Id,
                    Name = o.Coupon.Name,
                    Rate = o.Coupon.Rate,
                    IsPercentage = o.Coupon.IsPercentage
                } : null,
                DeliveryType = new DeliveryTypeDto
                {
                    Id = o.DeliveryType.Id,
                    Name = o.DeliveryType.Name,
                    Price = o.DeliveryType.Price
                },
                Phone = o.Phone,
                Status = o.Status.ToString(),
                DeliveryPrice = o.DeliveryPrice,
                TotalPrice = o.TotalPrice,
                CreatedAt = o.CreateTime,
                UpdatedAt = o.UpdatedAt
            });

            var response = new PaginationResponse<OrderDto>(pageSize, pageIndex, count, orderDtos);
            return new OkObjectResult(response);
        }

        // ========== Get Active Order ==========
        public async Task<IActionResult> GetUserActiveOrdersAsync(string userId, int pageIndex, int pageSize)
        {
            var spec = new OrderSpecifications.GetUserActiveOrdersSpec(userId, pageIndex, pageSize);
            var orders = await _unitOfWork.Repository<Order, int>().GetAllWithSpecficationAsync(spec);
            var count = await _unitOfWork.Repository<Order, int>().GetCountAsync(new OrderSpecifications.GetUserActiveOrdersCountSpec(userId));

            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                Address = new OrderAddressDto
                {
                    Street = o.Address.Street,
                    City = o.Address.City,
                    Government = o.Address.Government
                },
                Coupon = o.Coupon != null ? new CouponDto
                {
                    Id = o.Coupon.Id,
                    Name = o.Coupon.Name,
                    Rate = o.Coupon.Rate,
                    IsPercentage = o.Coupon.IsPercentage
                } : null,
                DeliveryType = new DeliveryTypeDto
                {
                    Id = o.DeliveryType.Id,
                    Name = o.DeliveryType.Name,
                    Price = o.DeliveryType.Price
                },
                Phone = o.Phone,
                Status = o.Status.ToString(),
                DeliveryPrice = o.DeliveryPrice,
                TotalPrice = o.TotalPrice,
                CreatedAt = o.CreateTime,
                UpdatedAt = o.UpdatedAt
            });

            var response = new PaginationResponse<OrderDto>(pageSize, pageIndex, count, orderDtos);
            return new OkObjectResult(response);
        }


        // ========== Get User Notifications (Updated with Media URL Handling) ==========
        public async Task<IActionResult> GetUserNotificationsAsync(string userId, int pageIndex, int pageSize)
        {
            var spec = new NotificationSpecifications.GetUserNotificationsSpec(userId, pageIndex, pageSize);
            var notifications = await _unitOfWork.Repository<Notification, int>().GetAllWithSpecficationAsync(spec);
            var count = await _unitOfWork.Repository<Notification, int>().GetCountAsync(new NotificationSpecifications.GetUserNotificationsCountSpec(userId));

            // Mark all user notifications as read
            var allUserNotificationsSpec = new NotificationSpecifications.GetUserNotificationsCountSpec(userId);
            var allUserNotifications = await _unitOfWork.Repository<Notification, int>().GetAllWithSpecficationAsync(allUserNotificationsSpec);

            var unreadNotifications = allUserNotifications.Where(n => !n.IsRead).ToList();//get unread notifications only and mark them as read
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                _unitOfWork.Repository<Notification, int>().Update(notification);
            }

            if (unreadNotifications.Any())
            {
                await _unitOfWork.CompleteAsync();
            }

            var baseUrl = _cfg["BaseURL"]?.TrimEnd('/') ?? "";
            var notificationDtos = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                SenderId = n.SenderId,
                SenderName = GetSenderName(n.SenderId),
                Title = n.Title,
                Message = n.Message,
                MediaUrl = !string.IsNullOrEmpty(n.MediaUrl) ? n.MediaUrl : null,
                Type = n.Type.ToString(),
                IsRead = true, // All notifications are now read
                CreatedAt = n.CreateTime
            });

            var response = new PaginationResponse<NotificationDto>(pageSize, pageIndex, count, notificationDtos);
            return new OkObjectResult(response);
        }

        // ========== Get Unread Notifications Count ==========
        public async Task<IActionResult> GetUnreadNotificationsCountAsync(string userId)
        {
            var spec = new NotificationSpecifications.GetUserUnreadNotificationsCountSpec(userId);
            var unreadCount = await _unitOfWork.Repository<Notification, int>().GetCountAsync(spec);

            return new OkObjectResult(new { UnreadCount = unreadCount });
        }

        // ========== Cancel Order ==========
        public async Task<IActionResult> CancelOrderAsync(string userId, int orderId)
        {
            var spec = new OrderSpecifications.GetOrderByIdAndUserSpec(orderId, userId);
            var order = await _unitOfWork.Repository<Order, int>().GetWithSpecficationAsync(spec);

            if (order == null)
                return new NotFoundObjectResult(new ErrorResponseDto { Message = "Order not found" });

            if (order.Status != OrderStatus.Pending)
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "Only pending orders can be cancelled" });

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Order, int>().Update(order);
            await _unitOfWork.CompleteAsync();

            return new OkObjectResult(new SuccessResponseDto { Message = "Order cancelled successfully" });
        }

        // ========== Update Phone ==========
        public async Task<IActionResult> UpdatePhoneAsync(string currentUserId, UpdatePhoneDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "Invalid phone number format" });

            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new NotFoundObjectResult(new ErrorResponseDto { Message = "User not found" });

            // Check if phone number is already taken
            var existingUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != currentUserId);
            if (existingUser != null)
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "Phone number is already in use" });

            user.PhoneNumber = dto.PhoneNumber;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return new BadRequestObjectResult(new ErrorResponseDto
                {
                    Message = "Failed to update phone number",
                    Errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                });

            return new OkObjectResult(new SuccessResponseDto { Message = "Phone number updated successfully" });
        }

        // ========== Get All Products ==========
        public async Task<IActionResult> GetAllProductsAsync(int pageIndex, int pageSize)
        {
            var spec = new ProductSpecifications.GetAllProductsSpec(pageIndex, pageSize);
            var products = await _unitOfWork.Repository<Product, int>().GetAllWithSpecficationAsync(spec);
            var count = await _unitOfWork.Repository<Product, int>().GetCountAsync(new ProductSpecifications.GetProductsCountSpec());

            var productDtos = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price
            });

            var response = new PaginationResponse<ProductDto>(pageSize, pageIndex, count, productDtos);
            return new OkObjectResult(response);
        }

        // ========== Get All Types of Service ==========
        public async Task<IActionResult> GetAllTypesOfServiceAsync(int pageIndex, int pageSize)
        {
            var spec = new ServiceSpecifications.GetAllServicesSpec(pageIndex, pageSize);
            var services = await _unitOfWork.Repository<TypeOfService, int>().GetAllWithSpecficationAsync(spec);
            var count = await _unitOfWork.Repository<TypeOfService, int>().GetCountAsync(new ServiceSpecifications.GetServicesCountSpec());

            var serviceDtos = services.Select(s => new TypeOfServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price
            });

            var response = new PaginationResponse<TypeOfServiceDto>(pageSize, pageIndex, count, serviceDtos);
            return new OkObjectResult(response);
        }

        // ========== Get All Delivery Types ==========
        public async Task<IActionResult> GetAllDeliveryTypesAsync(int pageIndex, int pageSize)
        {
            var spec = new DeliveryTypeSpecifications.GetAllDeliveryTypesSpec(pageIndex, pageSize);
            var deliveryTypes = await _unitOfWork.Repository<DeliveryType, int>()
                .GetAllWithSpecficationAsync(spec);

            var count = await _unitOfWork.Repository<DeliveryType, int>()
                .GetCountAsync(new DeliveryTypeSpecifications.GetDeliveryTypesCountSpec());

            var deliveryTypeDtos = deliveryTypes.Select(d => new DeliveryTypeDto
            {
                Id = d.Id,
                Name = d.Name,
                Price = d.Price
            });

            var response = new PaginationResponse<DeliveryTypeDto>(pageSize, pageIndex, count, deliveryTypeDtos);
            return new OkObjectResult(response);
        }

        // ========== Get User Address ==========
        public async Task<IActionResult> GetUserAddressAsync(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return new NotFoundObjectResult(new ErrorResponseDto { Message = "User not found" });

            if (user.Address == null)
                return new NotFoundObjectResult(new ErrorResponseDto { Message = "Address not found" });

            var addressDto = new AddressDto
            {
                Id = user.Address.Id,
                Street = user.Address.Street,
                City = user.Address.City,
                Government = user.Address.Government
            };

            return new OkObjectResult(addressDto);
        }

        // ========== Update User Address ==========
        public async Task<IActionResult> UpdateUserAddressAsync(string userId, UpdateAddressDto dto)
        {
            if (dto == null)
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "Invalid address data" });

            var user = await _userManager.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return new NotFoundObjectResult(new ErrorResponseDto { Message = "User not found" });

            if (user.Address == null)
            {
                // Create new address
                var newAddress = new Address
                {
                    Street = dto.Street,
                    City = dto.City,
                    Government = dto.Government
                };

                await _unitOfWork.Repository<Address, int>().AddAsync(newAddress);
                await _unitOfWork.CompleteAsync(); // Save address first to get the ID

                // Now update user with the address ID
                user.AddressId = newAddress.Id;
                user.Address = newAddress;
                var result = await _userManager.UpdateAsync(user); // This is crucial!

                if (!result.Succeeded)
                {
                    return new BadRequestObjectResult(new ErrorResponseDto
                    {
                        Message = "Failed to update user address",
                        Errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                    });
                }
            }
            else
            {
                // Update existing address
                user.Address.Street = dto.Street;
                user.Address.City = dto.City;
                user.Address.Government = dto.Government;

                _unitOfWork.Repository<Address, int>().Update(user.Address);
                await _unitOfWork.CompleteAsync();
            }

            return new OkObjectResult(new SuccessResponseDto { Message = "Address updated successfully" });
        }

        // ========== Update FCM Token ==========
        public async Task<IActionResult> UpdateFirstNameAsync(string currentUserId, UpdateNameDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.FirstName))
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "Invalid first name" });

            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new NotFoundObjectResult(new ErrorResponseDto { Message = "User not found" });

            user.FirstName = dto.FirstName.Trim();
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return new BadRequestObjectResult(new ErrorResponseDto
                {
                    Message = "Failed to update first name",
                    Errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                });

            return new OkObjectResult(new SuccessResponseDto { Message = "First name updated successfully" });
        }
        public async Task<IActionResult> UpdateLastNameAsync(string currentUserId, UpdateLastNameDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.LastName))
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "Invalid last name" });

            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new NotFoundObjectResult(new ErrorResponseDto { Message = "User not found" });

            user.LastName = dto.LastName.Trim();
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return new BadRequestObjectResult(new ErrorResponseDto
                {
                    Message = "Failed to update last name",
                    Errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                });

            return new OkObjectResult(new SuccessResponseDto { Message = "Last name updated successfully" });
        }

        // ========== set and update password ==========
        public async Task<IActionResult> SetPasswordAsync(string currentUserId, SetPasswordDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Password))
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "Invalid password" });

            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new NotFoundObjectResult(new ErrorResponseDto { Message = "User not found" });

            // Check if user already has a password
            if (await _userManager.HasPasswordAsync(user))
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "User already has a password. Use update-password endpoint instead." });

            var result = await _userManager.AddPasswordAsync(user, dto.Password);

            if (!result.Succeeded)
                return new BadRequestObjectResult(new ErrorResponseDto
                {
                    Message = "Failed to set password",
                    Errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                });

            return new OkObjectResult(new SuccessResponseDto { Message = "Password set successfully" });
        }
        public async Task<IActionResult> UpdatePasswordAsync(string currentUserId, UpdatePasswordDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "Invalid password data" });

            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new NotFoundObjectResult(new ErrorResponseDto { Message = "User not found" });

            // Check if user has a password
            if (!await _userManager.HasPasswordAsync(user))
                return new BadRequestObjectResult(new ErrorResponseDto { Message = "User doesn't have a password. Use set-password endpoint instead." });

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
                return new BadRequestObjectResult(new ErrorResponseDto
                {
                    Message = "Failed to update password",
                    Errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                });

            return new OkObjectResult(new SuccessResponseDto { Message = "Password updated successfully" });
        }

        // ========== Update FCM Token ==========
        public async Task<IActionResult> UpdateFCMTokenAsync(string userId, UpdateFCMTokenDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new NotFoundObjectResult(new ErrorResponseDto { Message = "User not found" });

            user.FCMToken = dto.FCMToken;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return new BadRequestObjectResult(new ErrorResponseDto
                {
                    Message = "Failed to update FCM token",
                    Errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                });

            return new OkObjectResult(new SuccessResponseDto { Message = "FCM token updated successfully" });
        }

        // ========== Helper ==========
        private string GetSenderName(string senderId)
        {
            var user = _userManager.FindByIdAsync(senderId).Result;
            if (user == null) return "System";

            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrEmpty(fullName) ? user.Email ?? "Admin" : fullName;
        }
    }
}
