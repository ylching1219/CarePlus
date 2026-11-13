
using CarePlus.API.Extension;
using System.Net;

namespace CarePlus.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        public static Task HandleExceptionAsync(HttpContext context , Exception exception)
        {
            context.Response.ContentType = "application/json";
            int statusCode;
            object? resData = null;
            string message = exception.Message ?? "An unexpected error occurred.";

            switch (exception)
            {
                case ValidationException valEx:
                    statusCode = (int)HttpStatusCode.BadRequest;

                    if (valEx.Errors.Any())
                    {
                        resData = valEx.Errors;
                        message = valEx.Errors.Values.SelectMany(x => x).FirstOrDefault() ?? valEx.Message;
                    }
                    break;
                case KeyNotFoundException: statusCode = (int)HttpStatusCode.NotFound;break;
                case UnauthorizedAccessException: statusCode = (int)HttpStatusCode.Unauthorized;break;
                case ArgumentException: statusCode = (int)HttpStatusCode.BadRequest;break;
                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    break;

            }
             

            var response = ApiResponse<object>.Fail(message,statusCode,resData);

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
