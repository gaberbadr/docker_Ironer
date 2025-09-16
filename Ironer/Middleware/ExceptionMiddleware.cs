using System.Text.Json;
using Ironer.Errors;

namespace Ironer.Middleware
{
    public class ExceptionMiddleware
    // i must add this middel ware in program app.UseMiddleware<ExceptionMiddleware>();
    //by adding the middle ware if any Exception happend this will run this calss,like i put my project in try catch
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var response = _env.IsDevelopment()
                    ? new ApiExceptionResponse(StatusCodes.Status500InternalServerError, ex.Message, ex.StackTrace?.ToString())//for developer
                    : new ApiExceptionResponse(StatusCodes.Status500InternalServerError);//for user

                var option = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, option);//turn my object to json

                await context.Response.WriteAsync(json);
            }
        }
    }
}
