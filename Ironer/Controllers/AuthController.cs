using System.Security.Claims;
using System.Web;
using CoreLayer.Dtos;
using CoreLayer.Entities.Identity;
using CoreLayer.Helper.Pagination;
using CoreLayer.Service_contract;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Data.Context;
using ServiceLayer.Services.Auth.Jwt;

namespace Ironer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _cfg;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext db,
            IEmailSender emailSender,
            IJwtService jwtService,
            IConfiguration cfg)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _emailSender = emailSender;
            _jwtService = jwtService;
            _cfg = cfg;
        }

        // ========== Send OTP Code ==========
        [HttpPost("send-verification-code")]
        public async Task<IActionResult> SendVerificationCode([FromBody] EmailDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Invalid email format",
                    Errors = ModelState.ToDictionary(
                        x => x.Key,
                        x => x.Value?.Errors?.Select(e => e.ErrorMessage).ToArray() ?? new string[0]
                    )
                });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Email = dto.Email,
                        UserName = dto.Email,
                        EmailConfirmed = false
                    };
                    await _userManager.CreateAsync(user);
                }

                var code = new Random().Next(100000, 999999).ToString();
                user.VerificationCode = code;
                user.CodeExpiresAt = DateTime.UtcNow.AddMinutes(1); // OTP valid for 1 minute
                await _userManager.UpdateAsync(user);

                await _emailSender.SendEmailAsync(dto.Email, "Verification Code",
                    $"Your verification code is {code}. It will expire in 1 minute.");

                return Ok(new SuccessResponseDto { Message = "Verification code sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "Failed to send verification code. Please try again."
                });
            }
        }

        // ========== Verify OTP & SignIn ==========
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Invalid input",
                    Errors = ModelState.ToDictionary(
                        x => x.Key,
                        x => x.Value?.Errors?.Select(e => e.ErrorMessage).ToArray() ?? new string[0]
                    )
                });
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized(new ErrorResponseDto { Message = "Invalid email" });

            if (user.CodeExpiresAt < DateTime.UtcNow)
                return BadRequest(new ErrorResponseDto { Message = "Verification code expired. Please request a new one." });

            if (user.VerificationCode != dto.Code)
                return BadRequest(new ErrorResponseDto { Message = "Invalid verification code." });

            // Confirm email
            user.EmailConfirmed = true;
            user.VerificationCode = null;
            await _userManager.UpdateAsync(user);

            // Generate tokens
            var (accessToken, accessExp) = await _jwtService.GenerateAccessTokenAsync(user, _userManager);

            // Reuse old refresh token if exists
            var existingToken = (await _db.RefreshTokens
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.ExpiresAt)
                .ToListAsync())
                .FirstOrDefault(t => t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow);

            string refreshToken;
            DateTime refreshExp;

            if (existingToken != null)
            {
                refreshToken = existingToken.Token;
                refreshExp = existingToken.ExpiresAt;
            }
            else
            {
                (refreshToken, refreshExp) = _jwtService.GenerateRefreshToken();
                _db.RefreshTokens.Add(new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = refreshExp,
                    CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _db.SaveChangesAsync();
            }

            return Ok(new TokenResponseDto
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessExp,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshExp
            });
        }

        // ========== Google login (optional) ==========
        [HttpGet("google-login")]// window.location.href = `${API_BASE}/api/auth/google-login?returnUrl=${encodeURIComponent('http://127.0.0.1:5500/callback.html')}`;});//i send the returnUrl(callback) url from here
        public IActionResult GoogleLogin([FromQuery] string returnUrl = "")
        {
            var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });//https://localhost:7061/Auth/GoogleCallback?returnUrl=http://127.0.0.1:5500/callback.html  (#1)
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);//send to Google, then redirect to GoogleCallback
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string returnUrl = "")//(#1.1) after google authentication it will come here i get data from google and generate tokens
        {
            try
            {
                var frontendUrl = _cfg["Frontend:BaseUrl"];
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    var errorUrl = string.IsNullOrEmpty(returnUrl)
                        ? $"{frontendUrl}/signin.html?error={HttpUtility.UrlEncode("External login info not found")}"
                        : $"{returnUrl}?error={HttpUtility.UrlEncode("External login info not found")}";
                    return Redirect(errorUrl);
                }

                var externalSignIn = await _signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

                ApplicationUser user;

                if (externalSignIn.Succeeded)
                {
                    user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                }
                else
                {
                    var email = info.Principal.FindFirstValue(ClaimTypes.Email);

                    if (email == null)
                    {
                        var errorUrl = string.IsNullOrEmpty(returnUrl)
                            ? $"{frontendUrl}/signin.html?error={HttpUtility.UrlEncode("Email not provided by Google")}"
                            : $"{returnUrl}?error={HttpUtility.UrlEncode("Email not provided by Google")}";
                        return Redirect(errorUrl);
                    }

                    user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            EmailConfirmed = true
                            // Remove FirstName and LastName - user will set them manually
                        };

                        var createResult = await _userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                            var errorUrl = string.IsNullOrEmpty(returnUrl)
                                ? $"{frontendUrl}/signin.html?error={HttpUtility.UrlEncode(errors)}"
                                : $"{returnUrl}?error={HttpUtility.UrlEncode(errors)}";
                            return Redirect(errorUrl);
                        }
                    }
                    else
                    {
                        if (!await _userManager.IsEmailConfirmedAsync(user))
                        {
                            user.EmailConfirmed = true;
                            await _userManager.UpdateAsync(user);
                        }
                        // Don't update first name and last name from Google
                    }

                    var logins = await _userManager.GetLoginsAsync(user);
                    if (!logins.Any(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey))
                    {
                        var addLogin = await _userManager.AddLoginAsync(user, info);
                        if (!addLogin.Succeeded)
                        {
                            var errors = string.Join(", ", addLogin.Errors.Select(e => e.Description));
                            var errorUrl = string.IsNullOrEmpty(returnUrl)
                                ? $"{frontendUrl}/signin.html?error={HttpUtility.UrlEncode(errors)}"
                                : $"{returnUrl}?error={HttpUtility.UrlEncode(errors)}";
                            return Redirect(errorUrl);
                        }
                    }
                }

                // Rest of token generation code remains the same...
                var (accessToken, accessExp) = await _jwtService.GenerateAccessTokenAsync(user, _userManager);

                var existingToken = (await _db.RefreshTokens
                    .Where(t => t.UserId == user.Id)
                    .OrderByDescending(t => t.ExpiresAt)
                    .ToListAsync())
                    .FirstOrDefault(t => t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow);

                string refreshToken;
                DateTime refreshExp;

                if (existingToken != null)
                {
                    refreshToken = existingToken.Token;
                    refreshExp = existingToken.ExpiresAt;
                }
                else
                {
                    (refreshToken, refreshExp) = _jwtService.GenerateRefreshToken();
                    _db.RefreshTokens.Add(new RefreshToken
                    {
                        Token = refreshToken,
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = refreshExp,
                        CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });
                    await _db.SaveChangesAsync();
                }

                var targetUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : $"{frontendUrl}/callback.html";
                var callbackUrl =
                    $"{targetUrl}?accessToken={Uri.EscapeDataString(accessToken)}" +
                    $"&refreshToken={Uri.EscapeDataString(refreshToken)}" +
                    $"&accessTokenExpiresAt={Uri.EscapeDataString(accessExp.ToString("O"))}" +
                    $"&refreshTokenExpiresAt={Uri.EscapeDataString(refreshExp.ToString("O"))}";

                return Redirect(callbackUrl);
            }
            catch (Exception ex)
            {
                var frontendUrl = _cfg["Frontend:BaseUrl"];
                var errorUrl = string.IsNullOrEmpty(returnUrl)
                    ? $"{frontendUrl}/signin.html?error={HttpUtility.UrlEncode(ex.Message)}"
                    : $"{returnUrl}?error={HttpUtility.UrlEncode(ex.Message)}";
                return Redirect(errorUrl);
            }
        }

        // ========== Phone and Password Login ==========
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [HttpPost("phone-login")]
        public async Task<IActionResult> PhonePasswordLogin([FromBody] PhonePasswordLoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Invalid input",
                    Errors = ModelState.ToDictionary(
                        x => x.Key,
                        x => x.Value?.Errors?.Select(e => e.ErrorMessage).ToArray() ?? new string[0]
                    )
                });
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);
            if (user == null)
                return Unauthorized(new ErrorResponseDto { Message = "Invalid phone number or password" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
                return Unauthorized(new ErrorResponseDto { Message = "Invalid phone number or password" });

            // Generate tokens
            var (accessToken, accessExp) = await _jwtService.GenerateAccessTokenAsync(user, _userManager);

            // Reuse old refresh token if exists
            var existingToken = (await _db.RefreshTokens
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.ExpiresAt)
                .ToListAsync())
                .FirstOrDefault(t => t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow);

            string refreshToken;
            DateTime refreshExp;

            if (existingToken != null)
            {
                refreshToken = existingToken.Token;
                refreshExp = existingToken.ExpiresAt;
            }
            else
            {
                (refreshToken, refreshExp) = _jwtService.GenerateRefreshToken();
                _db.RefreshTokens.Add(new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = refreshExp,
                    CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _db.SaveChangesAsync();
            }

            return Ok(new TokenResponseDto
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessExp,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshExp
            });
        }

        // ========== Me ==========
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ErrorResponseDto { Message = "User not authenticated" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new ErrorResponseDto { Message = "User not found" });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            });
        }

        // ========== Refresh Token ==========
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Invalid request",
                    Errors = ModelState.ToDictionary(
                        x => x.Key,
                        x => x.Value?.Errors?.Select(e => e.ErrorMessage).ToArray() ?? new string[0]
                    )
                });

            var tokenEntry = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.Token == request.RefreshToken);
            if (tokenEntry == null || !tokenEntry.IsActive)
                return Unauthorized(new ErrorResponseDto { Message = "Invalid refresh token." });

            var user = await _userManager.FindByIdAsync(tokenEntry.UserId);
            if (user == null)
                return Unauthorized(new ErrorResponseDto { Message = "Invalid token user." });

            // Revoke the old refresh token
            tokenEntry.RevokedAt = DateTime.UtcNow;

            // Generate new tokens
            var (accessToken, accessExp) = await _jwtService.GenerateAccessTokenAsync(user, _userManager);
            var (refreshToken, refreshExp) = _jwtService.GenerateRefreshToken();

            // Add new refresh token
            _db.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = refreshExp,
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            // Clean up expired refresh tokens for this user
            var expiredTokens = await _db.RefreshTokens
                .Where(t => t.UserId == user.Id && (t.ExpiresAt < DateTime.UtcNow || t.RevokedAt != null))
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _db.RefreshTokens.RemoveRange(expiredTokens);
            }

            await _db.SaveChangesAsync();

            return Ok(new TokenResponseDto
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessExp,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshExp
            });
        }

        // ========== Revoke Token ==========
        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [HttpPost("revoke-refresh")]
        public async Task<IActionResult> Revoke([FromBody] RefreshRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Invalid request",
                    Errors = ModelState.ToDictionary(
                        x => x.Key,
                        x => x.Value?.Errors?.Select(e => e.ErrorMessage).ToArray() ?? new string[0]
                    )
                });

            var tokenEntry = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.Token == request.RefreshToken);
            if (tokenEntry == null)
                return NotFound(new ErrorResponseDto { Message = "Refresh token not found." });

            tokenEntry.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new SuccessResponseDto { Message = "Refresh token revoked successfully." });
        }

        // ========== Logout (optional endpoint to revoke all user tokens) ==========
        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ErrorResponseDto { Message = "User not authenticated" });

            // Remove all refresh tokens for this user (expired, revoked, and active)
            var userTokens = await _db.RefreshTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (userTokens.Any())
            {
                _db.RefreshTokens.RemoveRange(userTokens);
                await _db.SaveChangesAsync();
            }

            return Ok(new SuccessResponseDto { Message = "Logged out successfully. All refresh tokens removed." });
        }
    }
}
