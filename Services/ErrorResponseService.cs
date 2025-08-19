using Learning_Project.DTOs;
using Learning_Project.Interfaces;
using Learning_Project.Models;

namespace Learning_Project.Services
{
    public class ErrorResponseService : IErrorResponseService
    {
        private readonly ILogger<ErrorResponseService> _logger;
        public ErrorResponseService(ILogger<ErrorResponseService> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// This method generates a standardized error response for validation failures.
        /// </summary>
        /// <param name="validationResult"></param>
        /// <param name="requestPath"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public ProblemDetailsData ErrorResponse(ValidationResult validationResult, string requestPath, int statusCode,string traceId)
        {
            try
            {
                ProblemDetailsData problemDetails = new ProblemDetailsData
                {
                    Type = "https://example.com/problem-details",
                    Title = "Validation Failed",
                    Detail = validationResult.ErrorMessage,
                    Instance = requestPath,
                    Status = statusCode,
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    TraceId = traceId,
                    ValidationErrors = new List<ValidationError>
                {
                    new ValidationError
                    {
                        FieldName = "name",
                        ErrorCode = validationResult.ErrorCode,
                        ErrorMessage = validationResult.ErrorMessage
                    }
                }
                };
                _logger.LogWarning("Validation failed for TraceId: {TraceId} RequestPath: {RequestPath} Error:{ErrorMessage}",traceId,requestPath, validationResult.ErrorMessage);
                return problemDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating error response");
                return CreateServerErrorResponse(ex, requestPath);
            }
        }
        /// <summary>
        /// This method creates standard server error responses 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="requestPath"></param>
        /// <returns></returns>
        public ProblemDetailsData CreateServerErrorResponse(Exception exception, string requestPath)
        {
            try
            {
                _logger.LogError("An error occured:{Message}", exception.Message);
                return CreateServerErrorResponse(exception, requestPath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return CreateServerErrorResponse(ex, requestPath);
            }
        }
    }
}
