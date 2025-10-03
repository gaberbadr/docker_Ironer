using System.Security.Claims;
using CoreLayer;
using CoreLayer.Dtos;
using CoreLayer.Entities.Identity;
using CoreLayer.Entities.Orders;
using CoreLayer.Helper.Documents;
using CoreLayer.Helper.Pagination;
using CoreLayer.Service_contract;
using CoreLayer.Specifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Data.Context;
using static CoreLayer.Entities.Enum.Enums;

namespace Ironer.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHost;
        private readonly IConfiguration _cfg;
        private readonly IFCMService _fcmService;
        private readonly AppDbContext _dbContext;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHost,
            IConfiguration cfg,
            IFCMService fcmService,
            AppDbContext dbContext)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHost = webHost;
            _cfg = cfg;
            _fcmService = fcmService;
            _dbContext = dbContext;
        }

        // ========== User Management (Admin & Admin Assistant) ==========

        [ProducesResponseType(typeof(PaginationResponse<UserDto>), StatusCodes.Status200OK)]
        [HttpGet("users")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var users = await _userManager.Users
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var count = await _userManager.Users.CountAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList(),
                    CreatedAt = DateTime.UtcNow // This would need proper tracking in ApplicationUser
                });
            }

            var response = new PaginationResponse<UserDto>(pageSize, pageIndex, count, userDtos);
            return Ok(response);
        }

        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [HttpGet("users/{id}")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new ErrorResponseDto { Message = "User not found" });

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList(),
                CreatedAt = DateTime.UtcNow
            };

            return Ok(userDto);
        }

        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [HttpGet("users/phone/{phone}")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetUserByPhone(string phone)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (user == null)
                return NotFound(new ErrorResponseDto { Message = "User not found" });

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList(),
                CreatedAt = DateTime.UtcNow
            };

            return Ok(userDto);
        }


        [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
        [HttpGet("users/{id}/address")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetUserAddressById(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new ErrorResponseDto { Message = "User not found" });

            if (user.Address == null)
                return NotFound(new ErrorResponseDto { Message = "User address not found" });

            var addressDto = new AddressDto
            {
                Id = user.Address.Id,
                Street = user.Address.Street,
                City = user.Address.City,
                Government = user.Address.Government
            };

            return Ok(addressDto);
        }

        [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
        [HttpGet("users/phone/{phone}/address")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetUserAddressByPhone(string phone)
        {
            var user = await _userManager.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phone);

            if (user == null)
                return NotFound(new ErrorResponseDto { Message = "User not found" });

            if (user.Address == null)
                return NotFound(new ErrorResponseDto { Message = "User address not found" });

            var addressDto = new AddressDto
            {
                Id = user.Address.Id,
                Street = user.Address.Street,
                City = user.Address.City,
                Government = user.Address.Government
            };

            return Ok(addressDto);
        }

        // ========== Admin Only User Management ==========

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpDelete("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new ErrorResponseDto { Message = "User not found" });

            // Check if user has Admin role
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = "You cannot delete a user with the Admin role"
                });
            }

            // Delete related data first (refresh tokens, notifications, orders)
            await DeleteUserRelatedData(id);

            // Delete the user
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Failed to delete user",
                    Errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                });

            // Save all changes
            await _unitOfWork.CompleteAsync();

            return Ok(new SuccessResponseDto { Message = "User deleted successfully" });
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPut("users/roles/{phone}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeUserRole(string phone, [FromBody] ChangeUserRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto { Message = "Invalid role" });

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (user == null)
                return NotFound(new ErrorResponseDto { Message = "User not found" });

            // Only allow Vip or Blacklist roles
            if (dto.Role != "Vip" && dto.Role != "Blacklist")
            {
                return BadRequest(new ErrorResponseDto { Message = "Only 'Vip' and 'Blacklist' roles are allowed" });
            }

            // Remove existing Vip and Blacklist roles only
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Where(r => r == "Vip" || r == "Blacklist").ToList();

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Failed to remove existing roles",
                        Errors = removeResult.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                    });
                }
            }

            // Add the new role (Vip or Blacklist)
            var addResult = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!addResult.Succeeded)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = $"Failed to add {dto.Role} role",
                    Errors = addResult.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                });
            }

            return Ok(new SuccessResponseDto { Message = $"User role updated to {dto.Role}" });
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPost("adminAssistants")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddAdminAssistant([FromBody] AddAdminAssistantDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto { Message = "Invalid phone number" });

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);
            if (user == null)
                return NotFound(new ErrorResponseDto { Message = "User not found" });

            var result = await _userManager.AddToRoleAsync(user, "AdminAssistant");
            if (!result.Succeeded)
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Failed to add admin assistant role",
                    Errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                });

            return Ok(new SuccessResponseDto { Message = "User promoted to Admin Assistant" });
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpDelete("users/roles/{phone}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveRoleByPhone(string phone)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (user == null)
                return NotFound(new ErrorResponseDto { Message = "User not found" });

            // Get all current roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Exclude Admin role from removal - cannot remove Admin role
            var rolesToRemove = currentRoles.Where(r => r != "Admin").ToList();

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Failed to remove user roles",
                        Errors = removeResult.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })
                    });
                }
            }

            var message = currentRoles.Contains("Admin")
                ? "All user roles removed successfully (Admin role preserved)"
                : "All user roles removed successfully";

            return Ok(new SuccessResponseDto { Message = message });
        }

        [ProducesResponseType(typeof(PaginationResponse<UserDto>), StatusCodes.Status200OK)]
        [HttpGet("adminAssistants")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAdminAssistants([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var adminAssistants = await _userManager.GetUsersInRoleAsync("AdminAssistant");
            var pagedAssistants = adminAssistants
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize);

            var assistantDtos = new List<UserDto>();
            foreach (var user in pagedAssistants)
            {
                var roles = await _userManager.GetRolesAsync(user);
                assistantDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            var response = new PaginationResponse<UserDto>(pageSize, pageIndex, adminAssistants.Count, assistantDtos);
            return Ok(response);
        }

        [ProducesResponseType(typeof(PaginationResponse<UserDto>), StatusCodes.Status200OK)]
        [HttpGet("users/roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsersByRole([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)//just vip and blacklist
        {
            var vipUsers = await _userManager.GetUsersInRoleAsync("Vip");
            var blacklistUsers = await _userManager.GetUsersInRoleAsync("Blacklist");

            var allRoleUsers = vipUsers.Concat(blacklistUsers).Distinct()
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize);

            var userDtos = new List<UserDto>();
            foreach (var user in allRoleUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            var totalCount = vipUsers.Count + blacklistUsers.Count;
            var response = new PaginationResponse<UserDto>(pageSize, pageIndex, totalCount, userDtos);
            return Ok(response);
        }

        // ========== Order Management ==========
        [ProducesResponseType(typeof(PaginationResponse<OrderDto>), StatusCodes.Status200OK)]
        [HttpGet("orders/pending")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetAllPendingOrders([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            // Get all pending orders with user information
            var spec = new OrderSpecifications.GetAllPendingOrdersVipFirstSpec(pageIndex, pageSize);
            var orders = await _unitOfWork.Repository<Order, int>().GetAllWithSpecficationAsync(spec);
            var count = await _unitOfWork.Repository<Order, int>().GetCountAsync(new OrderSpecifications.GetOrdersByStatusCountSpec(OrderStatus.Pending));

            // Sort orders: VIP users first, then others
            var sortedOrders = new List<Order>();
            var vipOrders = new List<Order>();
            var regularOrders = new List<Order>();

            foreach (var order in orders)
            {
                var user = await _userManager.FindByIdAsync(order.UserId);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Vip"))
                    {
                        vipOrders.Add(order);
                    }
                    else
                    {
                        regularOrders.Add(order);
                    }
                }
                else
                {
                    regularOrders.Add(order);
                }
            }

            // Combine VIP orders first, then regular orders
            sortedOrders.AddRange(vipOrders.OrderByDescending(o => o.CreateTime));
            sortedOrders.AddRange(regularOrders.OrderByDescending(o => o.CreateTime));

            var orderDtos = MapOrdersToDto(sortedOrders);
            var response = new PaginationResponse<OrderDto>(pageSize, pageIndex, count, orderDtos);
            return Ok(response);
        }

        [ProducesResponseType(typeof(PaginationResponse<OrderDto>), StatusCodes.Status200OK)]
        [HttpGet("orders/active")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetAllActiveOrders([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var activeStatuses = new[] { OrderStatus.Accepted, OrderStatus.Processing, OrderStatus.ReadyForPickup, OrderStatus.OutForDelivery, OrderStatus.Delivered };
            var spec = new OrderSpecifications.GetActiveOrdersSpec(activeStatuses, pageIndex, pageSize);
            var orders = await _unitOfWork.Repository<Order, int>().GetAllWithSpecficationAsync(spec);
            var count = await _unitOfWork.Repository<Order, int>().GetCountAsync(new OrderSpecifications.GetActiveOrdersCountSpec(activeStatuses));

            var orderDtos = MapOrdersToDto(orders);
            var response = new PaginationResponse<OrderDto>(pageSize, pageIndex, count, orderDtos);
            return Ok(response);
        }

        [ProducesResponseType(typeof(PaginationResponse<OrderDto>), StatusCodes.Status200OK)]
        [HttpGet("orders/history/all")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetAllOrdersHistory([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var spec = new OrderSpecifications.GetAllOrdersHistorySpec(pageIndex, pageSize);
            var orders = await _unitOfWork.Repository<Order, int>().GetAllWithSpecficationAsync(spec);
            var count = await _unitOfWork.Repository<Order, int>().GetCountAsync(new OrderSpecifications.GetAllOrdersCountSpec());

            var orderDtos = MapOrdersToDto(orders);
            var response = new PaginationResponse<OrderDto>(pageSize, pageIndex, count, orderDtos);
            return Ok(response);
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPut("orders/{id}/status")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> EditOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto { Message = "Invalid status" });

            if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var newStatus))
                return BadRequest(new ErrorResponseDto { Message = "Invalid order status" });

            var spec = new OrderSpecifications.GetOrderByIdSpec(id);
            var order = await _unitOfWork.Repository<Order, int>().GetWithSpecficationAsync(spec);

            if (order == null)
                return NotFound(new ErrorResponseDto { Message = "Order not found" });

            // Admin assistants cannot set status to Paid or Cancelled
            if (User.IsInRole("AdminAssistant") && (newStatus == OrderStatus.Paid || newStatus == OrderStatus.Cancelled))
                return Forbid();

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Order, int>().Update(order);

            // Send notification if status is ReadyForPickup or OutForDelivery
            if (newStatus == OrderStatus.ReadyForPickup || newStatus == OrderStatus.OutForDelivery)
            {
                await SendOrderStatusNotification(order.UserId, order.Id, newStatus);//by notification massege and fcm
            }

            await _unitOfWork.CompleteAsync();

            return Ok(new SuccessResponseDto { Message = "Order status updated successfully" });
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPut("orders/{id}/price")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderPrice(int id, [FromBody] UpdateOrderPriceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto { Message = "Invalid price" });

            var spec = new OrderSpecifications.GetOrderByIdSpec(id);
            var order = await _unitOfWork.Repository<Order, int>().GetWithSpecficationAsync(spec);

            if (order == null)
                return NotFound(new ErrorResponseDto { Message = "Order not found" });

            order.TotalPrice = dto.TotalPrice;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Order, int>().Update(order);
            await _unitOfWork.CompleteAsync();

            return Ok(new SuccessResponseDto { Message = "Order price updated successfully" });
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpDelete("orders/{id}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelOrderById(int id)
        {
            var spec = new OrderSpecifications.GetOrderByIdSpec(id);
            var order = await _unitOfWork.Repository<Order, int>().GetWithSpecficationAsync(spec);

            if (order == null)
                return NotFound(new ErrorResponseDto { Message = "Order not found" });

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Order, int>().Update(order);
            await _unitOfWork.CompleteAsync();

            return Ok(new SuccessResponseDto { Message = "Order cancelled successfully" });
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpDelete("orders/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var spec = new OrderSpecifications.GetOrderByIdSpec(id);
            var order = await _unitOfWork.Repository<Order, int>().GetWithSpecficationAsync(spec);

            if (order == null)
                return NotFound(new ErrorResponseDto { Message = "Order not found" });

            // Delete order items first
            var orderItems = await _unitOfWork.Repository<ItemOrder, int>()
                .GetAllWithSpecficationAsync(new ItemOrderSpecifications.GetByOrderIdSpec(order.Id));

            foreach (var item in orderItems)
            {
                // Delete order products and services
                var orderProducts = await _unitOfWork.Repository<OrderProduct, int>()
                    .GetAllWithSpecficationAsync(new OrderProductSpecifications.GetByItemOrderIdSpec(item.Id));
                foreach (var product in orderProducts)
                {
                    _unitOfWork.Repository<OrderProduct, int>().Delete(product);
                }

                var orderServices = await _unitOfWork.Repository<OrderService, int>()
                    .GetAllWithSpecficationAsync(new OrderServiceSpecifications.GetByItemOrderIdSpec(item.Id));
                foreach (var service in orderServices)
                {
                    _unitOfWork.Repository<OrderService, int>().Delete(service);
                }

                _unitOfWork.Repository<ItemOrder, int>().Delete(item);
            }

            _unitOfWork.Repository<Order, int>().Delete(order);
            await _unitOfWork.CompleteAsync();

            return Ok(new SuccessResponseDto { Message = "Order deleted successfully" });
        }

        // ========== Product Management (Admin Only) ==========


        [HttpPost("products")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProduct([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto { Message = "Invalid product data" });

            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price
            };

            await _unitOfWork.Repository<Product, int>().AddAsync(product);
            await _unitOfWork.CompleteAsync();

            return Ok(new { ProductId = product.Id, Message = "Product added successfully" });
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpDelete("products/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _unitOfWork.Repository<Product, int>().GetAsync(id);
            if (product == null)
                return NotFound(new ErrorResponseDto { Message = "Product not found" });

            _unitOfWork.Repository<Product, int>().Delete(product);
            await _unitOfWork.CompleteAsync();

            return Ok(new SuccessResponseDto { Message = "Product deleted successfully" });
        }

        [ProducesResponseType(typeof(PaginationResponse<ProductDto>), StatusCodes.Status200OK)]
        [HttpGet("products")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllProducts([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50)
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
            return Ok(response);
        }

        // ========== Service Management (Admin Only) ==========

        [HttpPost("typesOfService")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddTypeOfService([FromBody] CreateTypeOfServiceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto { Message = "Invalid service data" });

            var service = new TypeOfService
            {
                Name = dto.Name,
                Price = dto.Price
            };

            await _unitOfWork.Repository<TypeOfService, int>().AddAsync(service);
            await _unitOfWork.CompleteAsync();

            return Ok(new { ServiceId = service.Id, Message = "Service added successfully" });
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpDelete("typesOfService/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTypeOfService(int id)
        {
            var service = await _unitOfWork.Repository<TypeOfService, int>().GetAsync(id);
            if (service == null)
                return NotFound(new ErrorResponseDto { Message = "Service not found" });

            _unitOfWork.Repository<TypeOfService, int>().Delete(service);
            await _unitOfWork.CompleteAsync();

            return Ok(new SuccessResponseDto { Message = "Service deleted successfully" });
        }

        [ProducesResponseType(typeof(PaginationResponse<TypeOfServiceDto>), StatusCodes.Status200OK)]
        [HttpGet("typesOfService")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllTypesOfService([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50)
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
            return Ok(response);
        }


        // ========== DeliveryType Management (Admin Only) ==========

        [HttpPost("deliveryTypes")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddDeliveryType([FromBody] CreateDeliveryTypeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto { Message = "Invalid delivery type data" });

            var deliveryType = new DeliveryType
            {
                Name = dto.Name,
                Price = dto.Price
            };

            await _unitOfWork.Repository<DeliveryType, int>().AddAsync(deliveryType);
            await _unitOfWork.CompleteAsync();

            return Ok(new { DeliveryTypeId = deliveryType.Id, Message = "Delivery type added successfully" });
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpDelete("deliveryTypes/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDeliveryType(int id)
        {
            var deliveryType = await _unitOfWork.Repository<DeliveryType, int>().GetAsync(id);
            if (deliveryType == null)
                return NotFound(new ErrorResponseDto { Message = "Delivery type not found" });

            _unitOfWork.Repository<DeliveryType, int>().Delete(deliveryType);
            await _unitOfWork.CompleteAsync();

            return Ok(new SuccessResponseDto { Message = "Delivery type deleted successfully" });
        }

        [ProducesResponseType(typeof(PaginationResponse<DeliveryTypeDto>), StatusCodes.Status200OK)]
        [HttpGet("deliveryTypes")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllDeliveryTypes([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50)
        {
            var spec = new DeliveryTypeSpecifications.GetAllDeliveryTypesSpec(pageIndex, pageSize);
            var deliveryTypes = await _unitOfWork.Repository<DeliveryType, int>().GetAllWithSpecficationAsync(spec);
            var count = await _unitOfWork.Repository<DeliveryType, int>().GetCountAsync(new DeliveryTypeSpecifications.GetDeliveryTypesCountSpec());

            var deliveryTypeDtos = deliveryTypes.Select(d => new DeliveryTypeDto
            {
                Id = d.Id,
                Name = d.Name,
                Price = d.Price
            });

            var response = new PaginationResponse<DeliveryTypeDto>(pageSize, pageIndex, count, deliveryTypeDtos);
            return Ok(response);
        }


        // ========== Coupon Management (Admin Only) ==========

        [ProducesResponseType(typeof(IEnumerable<Coupon>), StatusCodes.Status200OK)]
        [HttpGet("coupons")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCoupons()
        {
            var couponRepo = _unitOfWork.Repository<Coupon, int>();
            var coupons = await couponRepo.GetAllAsync();

            if (coupons == null || !coupons.Any())
                return NotFound(new ErrorResponseDto { Message = "No coupons found" });

            return Ok(coupons.Select(c => new
            {
                c.Id,
                c.Name,
                c.Rate,
                c.IsPercentage,
                c.ExpiresAt
            }));
        }

        [HttpPost("coupons")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddCoupon([FromBody] CreateCouponDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto { Message = "Invalid coupon data" });

            // Check if coupon name already exists
            var existingCoupons = await _unitOfWork.Repository<Coupon, int>().GetAllAsync();
            var existingCoupon = existingCoupons.FirstOrDefault(c => c.Name.ToLower() == dto.Name.ToLower());

            if (existingCoupon != null)
                return BadRequest(new ErrorResponseDto { Message = "A coupon with this name already exists" });

            var coupon = new Coupon
            {
                Name = dto.Name,
                Rate = dto.Rate,
                IsPercentage = dto.IsPercentage,
                ExpiresAt = dto.ExpiresAt
            };

            await _unitOfWork.Repository<Coupon, int>().AddAsync(coupon);
            await _unitOfWork.CompleteAsync();

            return Ok(new { CouponId = coupon.Id, Message = "Coupon added successfully" });
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpDelete("coupons/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var couponRepo = _unitOfWork.Repository<Coupon, int>();
            var coupon = await couponRepo.GetAsync(id);

            if (coupon == null)
                return NotFound(new ErrorResponseDto { Message = "Coupon not found" });

            couponRepo.Delete(coupon);
            await _unitOfWork.CompleteAsync();

            return Ok(new { Message = "Coupon deleted successfully" });
        }

        // ========== Notification Management ==========

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPost("notifications/phone/{phone}")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> SendNotificationToUserByPhone(string phone, [FromForm] SendNotificationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto { Message = "Invalid notification data" });

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (user == null)
                return NotFound(new ErrorResponseDto { Message = "User not found" });

            var senderId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            string? mediaUrl = null;

            // Handle media file upload based on type
            if (dto.MediaFile != null && dto.Type != "Message")
            {
                try
                {
                    var folderName = GetFolderNameByType(dto.Type);
                    var fileName = DocumentSetting.Upload(dto.MediaFile, folderName);
                    var baseUrl = _cfg["BaseURL"]?.TrimEnd('/') ?? "";
                    mediaUrl = $"{baseUrl}/files/{folderName}/{fileName}";
                }
                catch (Exception ex)
                {
                    return BadRequest(new ErrorResponseDto { Message = $"Failed to upload media file: {ex.Message}" });
                }
            }

            // Save notification to database
            var notification = new Notification
            {
                SenderId = senderId,
                ReceiverId = user.Id,
                Title = dto.Title,
                Message = dto.Message,
                MediaUrl = mediaUrl,
                Type = Enum.Parse<NotificationType>(dto.Type, true)
            };

            await _unitOfWork.Repository<Notification, int>().AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            // Send FCM notification
            bool fcmSent = false;
            if (!string.IsNullOrEmpty(user.FCMToken))
            {
                try
                {
                    var fcmData = new Dictionary<string, string>
            {
                { "notificationId", notification.Id.ToString() },
                { "type", dto.Type.ToLower() },
                { "senderId", senderId }
            };

                    // Add media URL to FCM data if present
                    if (!string.IsNullOrEmpty(mediaUrl))
                    {
                        fcmData.Add("mediaUrl", mediaUrl);
                    }

                    fcmSent = await _fcmService.SendNotificationAsync(
                        user.FCMToken,
                        dto.Title,
                        dto.Message,
                        fcmData
                    );
                }
                catch (Exception ex)
                {
                    // Log FCM error but don't fail the whole operation
                    Console.WriteLine($"FCM Error for user {user.Id}: {ex.Message}");
                }
            }

            var responseMessage = fcmSent
                ? "Notification sent successfully (including push notification)"
                : "Notification sent successfully (push notification failed or no FCM token)";

            return Ok(new SuccessResponseDto { Message = responseMessage });
        }

        [ProducesResponseType(typeof(PaginationResponse<NotificationDto>), StatusCodes.Status200OK)]
        [HttpGet("users/phone/{phone}/messages")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetUserMessagesByPhone(string phone, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (user == null)
                return NotFound(new ErrorResponseDto { Message = "User not found" });

            var spec = new NotificationSpecifications.GetUserMessagesSpec(user.Id, pageIndex, pageSize);
            var notifications = await _unitOfWork.Repository<Notification, int>().GetAllWithSpecficationAsync(spec);
            var count = await _unitOfWork.Repository<Notification, int>().GetCountAsync(new NotificationSpecifications.GetUserMessagesCountSpec(user.Id));

            var notificationDtos = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                SenderId = n.SenderId,
                SenderName = GetSenderName(n.SenderId),
                Title = n.Title,
                Message = n.Message,
                MediaUrl = n.MediaUrl,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                CreatedAt = n.CreateTime
            });

            var response = new PaginationResponse<NotificationDto>(pageSize, pageIndex, count, notificationDtos);
            return Ok(response);
        }

        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPost("notifications/all")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> SendNotificationToAllUsers([FromForm] SendNotificationToAllDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto { Message = "Invalid notification data" });

            var senderId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var users = await _userManager.Users.ToListAsync();
            string? mediaUrl = null;

            // Handle media file upload based on type
            if (dto.MediaFile != null && dto.Type != "Message")
            {
                try
                {
                    var folderName = GetFolderNameByType(dto.Type);
                    var fileName = DocumentSetting.Upload(dto.MediaFile, folderName);
                    var baseUrl = _cfg["BaseURL"]?.TrimEnd('/') ?? "";
                    mediaUrl = $"{baseUrl}/files/{folderName}/{fileName}";
                }
                catch (Exception ex)
                {
                    return BadRequest(new ErrorResponseDto { Message = $"Failed to upload media file: {ex.Message}" });
                }
            }

            // Create notifications for all users
            var notifications = new List<Notification>();
            var fcmTokens = new List<string>();

            foreach (var user in users)
            {
                var notification = new Notification
                {
                    SenderId = senderId,
                    ReceiverId = user.Id,
                    Title = dto.Title,
                    Message = dto.Message,
                    MediaUrl = mediaUrl,
                    Type = Enum.Parse<NotificationType>(dto.Type, true)
                };

                notifications.Add(notification);
                await _unitOfWork.Repository<Notification, int>().AddAsync(notification);

                // Collect FCM tokens for batch sending
                if (!string.IsNullOrEmpty(user.FCMToken))
                {
                    fcmTokens.Add(user.FCMToken);
                }
            }

            await _unitOfWork.CompleteAsync();

            // Send FCM notifications to all users with tokens
            bool fcmSent = false;
            int fcmSuccessCount = 0;

            if (fcmTokens.Any())
            {
                try
                {
                    var fcmData = new Dictionary<string, string>
            {
                { "type", dto.Type.ToLower() },
                { "senderId", senderId },
                { "broadcast", "true" }
            };

                    // Add media URL to FCM data if present
                    if (!string.IsNullOrEmpty(mediaUrl))
                    {
                        fcmData.Add("mediaUrl", mediaUrl);
                    }

                    fcmSent = await _fcmService.SendNotificationToMultipleAsync(
                        fcmTokens,
                        dto.Title,
                        dto.Message,
                        fcmData
                    );

                    fcmSuccessCount = fcmTokens.Count; // Approximate - the actual success count would need to be returned from FCMService
                }
                catch (Exception ex)
                {
                    // Log FCM error but don't fail the whole operation
                    Console.WriteLine($"FCM Broadcast Error: {ex.Message}");
                }
            }

            var responseMessage = fcmSent
                ? $"Notification sent to {users.Count} users successfully (push notifications sent to {fcmSuccessCount} devices)"
                : $"Notification sent to {users.Count} users successfully (push notifications failed or no FCM tokens)";

            return Ok(new SuccessResponseDto { Message = responseMessage });
        }
        // ========== Helper Methods ==========

        private string GetFolderNameByType(string type)
        {
            return type.ToLower() switch
            {
                "image" => "images",
                "video" => "videos",
                _ => "documents"
            };
        }

        private async Task SendOrderStatusNotification(string userId, int orderId, OrderStatus status)//for fcm and notification massege
        {
            try
            {
                var senderId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                var title = status == OrderStatus.ReadyForPickup ? "Order Ready for Pickup" : "Order Out for Delivery";
                var message = status == OrderStatus.ReadyForPickup
                    ? $"Your order #{orderId} is ready for pickup!"
                    : $"Your order #{orderId} is out for delivery!";

                // Save notification to database
                var notification = new Notification
                {
                    SenderId = senderId,
                    ReceiverId = userId,
                    Title = title,
                    Message = message,
                    Type = NotificationType.Message
                };

                await _unitOfWork.Repository<Notification, int>().AddAsync(notification);

                // Get user's FCM token and send push notification
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.FCMToken))
                {
                    var data = new Dictionary<string, string>
            {
                { "orderId", orderId.ToString() },
                { "status", status.ToString() },
                { "type", "order_status_update" }
            };

                    var fcmResult = await _fcmService.SendNotificationAsync(user.FCMToken, title, message, data);
                    if (!fcmResult)
                    {
                        // Log FCM failure but don't fail the whole operation
                        // You might want to implement retry logic or fallback mechanisms
                        Console.WriteLine($"Failed to send FCM notification to user {userId}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't let it break the order status update
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }
        private string GetSenderName(string senderId)
        {
            var user = _userManager.FindByIdAsync(senderId).Result;
            if (user == null) return "System";

            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrEmpty(fullName) ? user.Email ?? "Admin" : fullName;
        }

        private IEnumerable<OrderDto> MapOrdersToDto(IEnumerable<Order> orders)
        {
            return orders.Select(o => new OrderDto
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
                    IsPercentage = o.Coupon.IsPercentage,
                    IsActive = o.Coupon.IsActive,
                    ExpiresAt = o.Coupon.ExpiresAt
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
        }

        private async Task DeleteUserRelatedData(string userId)
        {
            // Get the user to access address information
            var user = await _userManager.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Id == userId);


            // Delete user's address if it exists
            if (user?.Address != null)
            {
                _unitOfWork.Repository<Address, int>().Delete(user.Address);
            }


            // Delete user refresh tokens
            var userRefreshTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .ToListAsync();

            if (userRefreshTokens.Any())
            {
                _dbContext.RefreshTokens.RemoveRange(userRefreshTokens);
            }

            // Delete user notifications
            var userNotifications = await _unitOfWork.Repository<Notification, int>()
                .GetAllWithSpecficationAsync(new NotificationSpecifications.GetByUserIdSpec(userId));

            foreach (var notification in userNotifications)
            {
                // Delete media files if they exist
                if (!string.IsNullOrEmpty(notification.MediaUrl))
                {
                    var fileName = Path.GetFileName(notification.MediaUrl);
                    var folderName = GetFolderNameByType(notification.Type.ToString());
                    DocumentSetting.Delete(fileName, folderName);
                }

                _unitOfWork.Repository<Notification, int>().Delete(notification);
            }

            // Delete user orders and related data
            var userOrders = await _unitOfWork.Repository<Order, int>()
                .GetAllWithSpecficationAsync(new OrderSpecifications.GetByUserIdSpec(userId));

            foreach (var order in userOrders)
            {
                // Delete order items first
                var orderItems = await _unitOfWork.Repository<ItemOrder, int>()
                    .GetAllWithSpecficationAsync(new ItemOrderSpecifications.GetByOrderIdSpec(order.Id));

                foreach (var item in orderItems)
                {
                    // Delete order products and services
                    var orderProducts = await _unitOfWork.Repository<OrderProduct, int>()
                        .GetAllWithSpecficationAsync(new OrderProductSpecifications.GetByItemOrderIdSpec(item.Id));
                    foreach (var product in orderProducts)
                    {
                        _unitOfWork.Repository<OrderProduct, int>().Delete(product);
                    }

                    var orderServices = await _unitOfWork.Repository<OrderService, int>()
                        .GetAllWithSpecficationAsync(new OrderServiceSpecifications.GetByItemOrderIdSpec(item.Id));
                    foreach (var service in orderServices)
                    {
                        _unitOfWork.Repository<OrderService, int>().Delete(service);
                    }

                    _unitOfWork.Repository<ItemOrder, int>().Delete(item);
                }

                _unitOfWork.Repository<Order, int>().Delete(order);
            }

            await _unitOfWork.CompleteAsync();
        }

    }
}
