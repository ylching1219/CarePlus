using CarePlus.API.Extension;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CarePlus.API.Filters
{
    public sealed class RejectDangerousMarkupFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null) continue;
                if (PlainTextGuard.FindDangerousValue(argument) is null) continue;

                context.Result = new BadRequestObjectResult(
                    ApiResponse<object>.Fail("Text fields cannot contain HTML or script characters.", 400));
                return;
            }

            await next();
        }
    }
}
