using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using static CoreLayer.Entities.Enum.Enums;

namespace CoreLayer.Entities.Orders
{
    [Owned]
    public class OrderAddress
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string Government { get; set; } = "";
    }
}
