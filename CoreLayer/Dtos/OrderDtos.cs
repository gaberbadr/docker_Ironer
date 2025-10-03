using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoreLayer.Dtos
{
    public class OrderProductDto
    {
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    public class OrderServiceDto
    {
        [JsonPropertyName("typeOfServiceId")]
        public int TypeOfServiceId { get; set; }

    }

    public class ItemOrderDto
    {
        [JsonPropertyName("products")]
        public List<OrderProductDto> Products { get; set; } = new List<OrderProductDto>();

        [JsonPropertyName("services")]
        public List<OrderServiceDto> Services { get; set; } = new List<OrderServiceDto>();

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";

    }

    public class OrderAddressDto
    {
        [JsonPropertyName("street")]
        public string Street { get; set; } = "";

        [JsonPropertyName("city")]
        public string City { get; set; } = "";

        [JsonPropertyName("government")]
        public string Government { get; set; } = "";
    }

    public class CreateOrderDto
    {
        [Required]
        [JsonPropertyName("address")]
        public OrderAddressDto Address { get; set; } = new OrderAddressDto();

        [Required]
        [Phone]
        [JsonPropertyName("Phone")]
        public string Phone { get; set; } = "";

        [Required]
        [JsonPropertyName("items")]
        public List<ItemOrderDto> Items { get; set; } = new List<ItemOrderDto>();

        [JsonPropertyName("couponName")]
        public string? CouponName { get; set; } 

        [Required]
        [JsonPropertyName("deliveryTypeId")]
        public int DeliveryTypeId { get; set; }
    }




    public class OrderDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = "";

        [JsonPropertyName("address")]
        public OrderAddressDto Address { get; set; } = new OrderAddressDto();

        [JsonPropertyName("items")]
        public List<ItemOrderDto> Items { get; set; } = new List<ItemOrderDto>();

        [JsonPropertyName("Phone")]
        public string Phone { get; set; } = "";

        [JsonPropertyName("coupon")]
        public CouponDto? Coupon { get; set; }

        [JsonPropertyName("deliveryType")]
        public DeliveryTypeDto DeliveryType { get; set; } = new DeliveryTypeDto();


        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("deliveryPrice")]
        public decimal DeliveryPrice { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }


    public class UpdateOrderStatusDto
    {
        [Required]
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";
    }

    public class UpdateOrderPriceDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total price must be greater than 0")]
        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }
    }

}
