using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Extension
{
    public static class ApiResultExtensions
    {
        // ponytail: map nested RespCode onto real HTTP status so WAF/clients see 401/423/etc.
        public static IActionResult ApiResult<T>(this ControllerBase controller, ApiResponse<T> response)
        {
            if (response.IsSuccess)
            {
                return controller.Ok(response);
            }

            var code = response.RespCode;
            if (code < 400 || code > 599)
            {
                code = 400;
            }

            return controller.StatusCode(code, response);
        }
    }
}
