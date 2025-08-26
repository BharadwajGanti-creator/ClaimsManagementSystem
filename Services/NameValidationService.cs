using Learning_Project.Interfaces;
using Learning_Project.Models;
using Learning_Project.Constants;
using System.Diagnostics;

namespace Learning_Project.Services
{
    public class NameValidationService : INameValidationService
    {
        private readonly ILogger<NameValidationService> _logger;
        public NameValidationService(ILogger<NameValidationService> logger)
        {
            _logger = logger;
        }
        public static HashSet<char> AllowedCharacters()
        {
            var chars = new HashSet<char>();
            for (char c = 'a'; c <= 'z'; c++)
            {
                chars.Add(c);
                chars.Add(char.ToUpper(c)); // Add uppercase letters
            }
            // Add space character
            chars.Add(' ');
            // Add hyphen character
            chars.Add('-');
            // Add apostrophe character
             chars.Add('\'');
            char apostrophe = '\'';
            Console.WriteLine($"Apostrophe: {apostrophe}");
            return new HashSet<char>(chars);
        }
        /// <summary>
        /// // This method validates the name based on the following criteria:
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ValidationResult ValidateName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogWarning("Name validation failed: Name is empty or whitespace.");
                    return ValidationResult.Failure(ValidationMessages.EMPTY_NAME, ValidationMessages.EMPTY_NAME_ERROR_CODE);
                }
                name = name.Trim();
                //Checking the length of the name
                if (name.Length < 1 || name.Length > 32)
                {
                    _logger.LogWarning("Name validation failed: Name length is invalid.");
                    return ValidationResult.Failure(ValidationMessages.LENGTH_VALIDATION, ValidationMessages.LENGTH_VALIDATION_ERROR_CODE);
                }
                //Checking for invalid characters
                if (!IsValidCharacter(AllowedCharacters(), name))
                {
                    _logger.LogWarning("Name validation failed: Name contains invalid characters.");
                    return ValidationResult.Failure(ValidationMessages.CHARACTER_VALIDATION, ValidationMessages.CHARACTER_VALIDATION_ERROR_CODE);
                }
                //Checking for consecutive spaces and starting/ending with non-letter characters
                if (HasConsecutiveSpaces(name) || StartOrEndsWithNonLetter(name))
                {
                    _logger.LogWarning("Name validation failed: Name contains consecutive spaces or starts/ends with non-letter characters.");
                    return ValidationResult.Failure(ValidationMessages.CONSECUTIVE_SPACES_VALIDATION, ValidationMessages.CONSECUTIVE_SPACES_VALIDATION_ERROR_CODE);
                }
                // If all checks pass, return success
                _logger.LogInformation("Name validation succeeded.");
                return ValidationResult.Success();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred during name validation.");
                return ValidationResult.Failure("An unexpected error occurred during name validation.", "UnexpectedError");
            }

        }
        /// <summary>
        /// // This method validates the name based on the following criteria:
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Task<ValidationResult> ValidateNameAsync(string name, CancellationToken  cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogWarning("Name validation failed: Name is empty or whitespace.");
                    return Task.FromResult(ValidationResult.Failure(ValidationMessages.EMPTY_NAME, ValidationMessages.EMPTY_NAME_ERROR_CODE));             
                }
                name = name.Trim();
                //Checking the length of the name
                if (name.Length < 1 || name.Length > 32)
                {
                    _logger.LogWarning("Name validation failed: Name length is invalid.");
                    return Task.FromResult(ValidationResult.Failure(ValidationMessages.LENGTH_VALIDATION, ValidationMessages.LENGTH_VALIDATION_ERROR_CODE));
                }
                //Checking for invalid characters
                if (!IsValidCharacter(AllowedCharacters(), name))
                {
                    _logger.LogWarning("Name validation failed: Name contains invalid characters.");
                    return Task.FromResult(ValidationResult.Failure(ValidationMessages.CHARACTER_VALIDATION, ValidationMessages.CHARACTER_VALIDATION_ERROR_CODE));
                }
                //Checking for consecutive spaces and starting/ending with non-letter characters
                if (HasConsecutiveSpaces(name) || StartOrEndsWithNonLetter(name))
                {
                    _logger.LogWarning("Name validation failed: Name contains consecutive spaces or starts/ends with non-letter characters.");
                    return Task.FromResult(ValidationResult.Failure(ValidationMessages.CONSECUTIVE_SPACES_VALIDATION, ValidationMessages.CONSECUTIVE_SPACES_VALIDATION_ERROR_CODE));
                }
                // If all checks pass, return success
                _logger.LogInformation("Name validation succeeded.");
                return Task.FromResult(ValidationResult.Success());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during name validation.");
                return Task.FromResult(ValidationResult.Failure("An unexpected error occurred during name validation.", "UnexpectedError"));
            }

        }
        // Check if the name contains only allowed characters
        private bool IsValidCharacter(HashSet<char> allowedCharacters, string name)
        {
            return name.All(c => allowedCharacters.Contains(c));
        }
        // Check for consecutive spaces
        private bool HasConsecutiveSpaces(string name)
        {
            return name.Contains("  "); // Check for consecutive spaces
        }
        // Check if the name starts or ends with a non-letter character
        private bool StartOrEndsWithNonLetter(string name)
            {
            return !char.IsLetter(name[0]) || !char.IsLetter(name[^1]);
            }
    }
}
