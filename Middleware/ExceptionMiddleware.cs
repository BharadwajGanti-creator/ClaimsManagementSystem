using Learning_Project.DTOs;
using Learning_Project.Interfaces;


namespace Learning_Project.Middleware
{
    public class ExceptionMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IErrorResponseService _errorResponseService;
        public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger, IErrorResponseService errorResponseService)
        {
            _logger = logger;
            _errorResponseService = errorResponseService;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while processing the request : {RequestPath}", context.Request.Path);
                var errorResponse = _errorResponseService.CreateServerErrorResponse(ex, context.Request.Path);
                errorResponse.Status = 500; // Internal Server Error
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(errorResponse);
            }
        }
    }
}
