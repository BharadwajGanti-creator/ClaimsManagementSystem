using Learning_Project.DTOs;
using Learning_Project.Models;

namespace Learning_Project.Interfaces
{
    public interface IErrorResponseService
    {
        public ProblemDetailsData ErrorResponse(ValidationResult validationResult, string requestPath, int statusCode, string traceId);
        public ProblemDetailsData CreateServerErrorResponse(Exception exception, string requestPath);
    }
}
