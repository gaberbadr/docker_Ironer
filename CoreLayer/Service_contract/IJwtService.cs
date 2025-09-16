using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace CoreLayer.Service_contract
{
    public interface IJwtService
    {
        Task<(string token, DateTime expiresAt)> GenerateAccessTokenAsync(
            ApplicationUser user,
            UserManager<ApplicationUser> userManager
        );

        (string token, DateTime expiresAt) GenerateRefreshToken();
    }

}
