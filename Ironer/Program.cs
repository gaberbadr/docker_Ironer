using System.Text;
using CoreLayer;
using CoreLayer.Entities.Identity;
using CoreLayer.Service_contract;
using Ironer.Errors;
using Ironer.Middleware;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using RepositoryLayer;
using RepositoryLayer.Data.Context;
using RepositoryLayer.Data.Data_seeding;
using ServiceLayer.Services.Auth.Jwt;
using ServiceLayer.Services.Auth.Notification;
using ServiceLayer.Services.Auth.Smtp;
using ServiceLayer.Services.User;
using System.Threading.RateLimiting;

namespace Ironer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //for .env (Docker) and local development
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true) // Load local secrets
                .AddEnvironmentVariables();

            // Get connection string and determine database type
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var useSqlite = string.IsNullOrEmpty(connectionString) || connectionString.Contains("Server=db") || connectionString.Contains("Server=.");

            // Use SQLite for standalone container, SQL Server for development/docker-compose
            if (builder.Environment.IsProduction())
            {
                // In production, try SQL Server first, fallback to SQLite
                try
                {
                    if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("Data Source="))
                    {
                        using var testConnection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                        await testConnection.OpenAsync();
                        testConnection.Close();
                        // SQL Server is available, use it
                        builder.Services.AddDbContext<AppDbContext>(options =>
                            options.UseSqlServer(connectionString));
                    }
                    else
                    {
                        throw new Exception("Use SQLite fallback");
                    }
                }
                catch
                {
                    // SQL Server not available, use SQLite
                    var sqliteConnectionString = "Data Source=ironer.db";
                    builder.Services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlite(sqliteConnectionString));
                }
            }
            else
            {
                // Development environment with SQL Server
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }

            // Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // services
            builder.Services.AddScoped<IEmailSender, EmailSender>();
            builder.Services.AddScoped<JwtService>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IFCMService, FCMService>();

            // JWT with fallback values
            var jwtKey = builder.Configuration["JWT:Key"] ?? "DefaultKeyForDevelopmentOnlyNotForProduction123";
            var key = Encoding.UTF8.GetBytes(jwtKey);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = builder.Configuration["JWT:Issuer"] ?? "http://localhost:5000/",
                    ValidAudience = builder.Configuration["JWT:Audience"] ?? "IronerAPI",
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
                options.Scope.Add("email");
                options.Scope.Add("profile");
            });

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Authorization policy to exclude blacklisted users
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("NotBlacklisted", policy =>
                {
                    policy.RequireAssertion(context =>
                        !context.User.IsInRole("Blacklist"));
                });
            });

            //validation error response
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = (actionContext) =>
                {
                    var errors = actionContext.ModelState
                        .Where(P => P.Value.Errors.Count() > 0)
                        .SelectMany(P => P.Value.Errors)
                        .Select(E => E.ErrorMessage)
                        .ToArray();

                    var response = new ApiValidationErrorResponse() { Errors = errors };
                    return new BadRequestObjectResult(response);
                };
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Rate Limiting - 15 requests per minute per IP
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 15,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));
            });

            var app = builder.Build();

            // Use CORS
            app.UseCors("AllowAll");

            //when app run, it automatically apply all migrations (Update DataBase)
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<AppDbContext>();
            var LoggerFactory = services.GetRequiredService<ILoggerFactory>();
            var usermanger = services.GetRequiredService<UserManager<ApplicationUser>>();

            try
            {
                await context.Database.MigrateAsync();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                IUnitOfWork _unitOfWork = services.GetRequiredService<IUnitOfWork>();
                await IdentitySeeder.SeedAppUserAsync(usermanger, roleManager, _unitOfWork);
            }
            catch (Exception ex)
            {
                var logger = LoggerFactory.CreateLogger<Program>();
                logger.LogError(ex, "Problem during migration, continuing with existing database");
                // Don't throw - continue running even if migration fails
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                // Always enable Swagger for easy testing in production
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ironer API V1");
                    c.RoutePrefix = "swagger";
                });
            }

            app.UseHttpsRedirection();

            // Enable rate limiting
            app.UseRateLimiter();

            app.UseAuthentication();

            // Enable serving static files from wwwroot (UPDATED FOR FILES SUPPORT)
            app.UseStaticFiles();

            // Enable serving files from wwwroot/files directory with proper MIME types
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files")),
                RequestPath = "/files",
                OnPrepareResponse = context =>
                {
                    var fileExtension = Path.GetExtension(context.File.Name).ToLowerInvariant();
                    context.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000"; // 1 year cache

                    // Set proper MIME types for media files
                    context.Context.Response.ContentType = fileExtension switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".gif" => "image/gif",
                        ".webp" => "image/webp",
                        ".mp4" => "video/mp4",
                        ".webm" => "video/webm",
                        ".avi" => "video/avi",
                        ".mov" => "video/quicktime",
                        ".pdf" => "application/pdf",
                        ".doc" => "application/msword",
                        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        _ => "application/octet-stream"
                    };
                }
            });

            app.UseAuthorization();

            // custom middleware to require phone for protected endpoints
            app.UseMiddleware<RequirePhoneNumberAndAddressMiddleware>();

            //this middleware happened when their an Exception, will run ExceptionMiddleware
            app.UseMiddleware<ExceptionMiddleware>();

            //this middleware if end point not found it redirect on Errors controller action error
            app.UseStatusCodePagesWithReExecute("/error/{0}");

            app.MapControllers();

            app.Run();
        }
    }
}