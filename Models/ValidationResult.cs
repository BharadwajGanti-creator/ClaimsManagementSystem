using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Learning_Project.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public ValidationResult()
        {
            IsValid = true;
            ErrorMessage = string.Empty;
            ErrorCode = string.Empty;
        }
        public ValidationResult(string errorMessage = "", string errorCode = "")
        {
            IsValid = false;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }
        //Static factory methods for common validation results
        public static ValidationResult Success() => new ValidationResult();

        public static ValidationResult Failure(string errorMessage, string errorCode = "") => new ValidationResult(errorMessage, errorCode);
    }
    }
