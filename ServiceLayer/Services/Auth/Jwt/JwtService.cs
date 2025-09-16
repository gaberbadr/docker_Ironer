using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CoreLayer.Entities.Identity;
using CoreLayer.Service_contract;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ServiceLayer.Services.Auth.Jwt
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _cfg;
        public JwtService(IConfiguration cfg) { _cfg = cfg; }

        public async Task<(string token, DateTime expiresAt)> GenerateAccessTokenAsync(ApplicationUser user, UserManager<ApplicationUser> userManager)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["JWT:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(double.Parse(_cfg["JWT:AccessTokenExpirationMinutes"] ?? "15"));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim("uid", user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim("phone", user.PhoneNumber ?? ""),
                new Claim("fname", user.FirstName ?? ""),
                new Claim("lname", user.LastName ?? "")
            };

            var userRoles = await userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }


            var token = new JwtSecurityToken(
                issuer: _cfg["JWT:Issuer"],
                audience: _cfg["JWT:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        public (string token, DateTime expiresAt) GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var token = Convert.ToBase64String(randomBytes);
            var expires = DateTime.UtcNow.AddDays(double.Parse(_cfg["JWT:RefreshTokenExpirationDays"] ?? "7"));
            return (token, expires);
        }
    }
}
