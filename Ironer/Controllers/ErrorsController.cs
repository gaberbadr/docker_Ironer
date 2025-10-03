using Ironer.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ironer.Controllers
{
    [Route("error/{code}")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorsController : Controller
    {
        public IActionResult Error(int code)
        {
            //#8 in program

            return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, "Not Found End Point !"));
        }
    }
}
