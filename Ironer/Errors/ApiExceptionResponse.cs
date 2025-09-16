namespace Ironer.Errors
{
    public class ApiExceptionResponse : ApiErrorResponse       //this class for Exception that happend in the program
    {
        public string? Details { get; set; }

        public ApiExceptionResponse(int statusCode, string? message = null, string? details = null) : base(statusCode: statusCode, message: message)
        {
            Details = details;
        }
    }
}
