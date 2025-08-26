using Learning_Project.Services;
using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Learning_Project.Constants;

namespace Learning_Project.Tests.UnitTests
{
    public class NameValidationServiceTests
    {
        private NameValidationService _validationService;
        [SetUp]
        public void SetUp()
        {
            var _mocklogger = new Mock<ILogger<NameValidationService>>();
        _validationService = new NameValidationService(_mocklogger.Object);
        }
        #region Success Scenarios
        [Test]
        public async Task ValidateNameAsync_ShouldReturnSameResultAs_SynchronousVersion()
        {
            //Test that async version produces identical results
            var validInput = "Get name";
            var syncResult = _validationService.ValidateName(validInput);
            var asyncResult = await _validationService.ValidateNameAsync(validInput);

            //Assert identical outcomes
            Assert.That(asyncResult.IsValid, Is.EqualTo(syncResult.IsValid));
            Assert.That(asyncResult.ErrorMessage, Is.EqualTo(syncResult.ErrorMessage));
            Assert.That(asyncResult.ErrorCode, Is.EqualTo(syncResult.ErrorCode));
        }
        #endregion
        #region Failure Test Scenarios
        [Test]
        public async Task ValidateNameAsync_ShouldHaveInvalidInput_SameAsSync()
        {
            //Test valid input
            var invalidInput = "";
            var syncResult = _validationService.ValidateName(invalidInput);
            var asyncResult = await _validationService.ValidateNameAsync(invalidInput);

            //Assert identical outcomes
            Assert.That(asyncResult.IsValid, Is.EqualTo(syncResult.IsValid));
            Assert.That(asyncResult.ErrorMessage, Is.EqualTo(syncResult.ErrorMessage));
            Assert.That(asyncResult.ErrorCode, Is.EqualTo(syncResult.ErrorCode));
        }
        [Test]
        public async Task ValidateNameAsync_WhenInputExceedsLength_ShouldReturnLengthValidationError()
        {
            //Test invalid input exceeding length limit
            var invalidLengthInput = "SriHariVenkataSaiKrishnaBharadwaj";
            var syncResult = _validationService.ValidateName(invalidLengthInput);
            var asyncResult = await _validationService.ValidateNameAsync(invalidLengthInput);

            //Assert identical outcomes using ValidationMessages constants
            Assert.That(asyncResult.IsValid, Is.EqualTo(syncResult.IsValid));
            Assert.That(asyncResult.ErrorCode, Is.EqualTo(syncResult.ErrorCode));
            Assert.That(asyncResult.ErrorMessage, Is.EqualTo(syncResult.ErrorMessage));

            //Verify expected ValidationMessages constants
            Assert.That(asyncResult.ErrorMessage, Is.EqualTo(ValidationMessages.LENGTH_VALIDATION));
            Assert.That(asyncResult.ErrorCode, Is.EqualTo(ValidationMessages.LENGTH_VALIDATION_ERROR_CODE));
        }
        [Test]
        public async Task ValidateNameAsync_WhenInputContainsSpecialChars_ShouldReturnCharacterValidationError()
        {
            //Test invalid input containing special characters
            var invalidLengthInput = "J@mes";
            var syncResult = _validationService.ValidateName(invalidLengthInput);
            var asyncResult = await _validationService.ValidateNameAsync(invalidLengthInput);

            //Assert identical outcomes using ValidationMessages constants
            Assert.That(asyncResult.IsValid, Is.EqualTo(syncResult.IsValid));
            Assert.That(asyncResult.ErrorCode, Is.EqualTo(syncResult.ErrorCode));
            Assert.That(asyncResult.ErrorMessage, Is.EqualTo(syncResult.ErrorMessage));

            //Verify expected ValidationMessages constants
            Assert.That(asyncResult.ErrorMessage, Is.EqualTo(ValidationMessages.CHARACTER_VALIDATION));
            Assert.That(asyncResult.ErrorCode, Is.EqualTo(ValidationMessages.CHARACTER_VALIDATION_ERROR_CODE));
        }
        [Test]
        public async Task ValidateNameAsync_WhenInputContainsConsecutiveSpaces_ShouldReturnSpacesValidationError()
        {
            //Test invalid input containing consecutive spaces
            var invalidLengthInput = "Test  Name";
            var syncResult = _validationService.ValidateName(invalidLengthInput);
            var asyncResult = await _validationService.ValidateNameAsync(invalidLengthInput);

            //Assert identical outcomes using ValidationMessages constants
            Assert.That(asyncResult.IsValid, Is.EqualTo(syncResult.IsValid));
            Assert.That(asyncResult.ErrorCode, Is.EqualTo(syncResult.ErrorCode));
            Assert.That(asyncResult.ErrorMessage, Is.EqualTo(syncResult.ErrorMessage));

            //Verify expected ValidationMessages constants
            Assert.That(asyncResult.ErrorMessage, Is.EqualTo(ValidationMessages.CONSECUTIVE_SPACES_VALIDATION));
            Assert.That(asyncResult.ErrorCode, Is.EqualTo(ValidationMessages.CONSECUTIVE_SPACES_VALIDATION_ERROR_CODE));
        }
        [Test]
        public async Task ValidateNameAsync_WhenInputViolatesStartEndRules_ShouldReturnStartEndValidationError()
        {
            //Test invalid input starting and ending with special characters
            var invalidLengthInput = "-Test-";
            var syncResult = _validationService.ValidateName(invalidLengthInput);
            var asyncResult = await _validationService.ValidateNameAsync(invalidLengthInput);

            //Assert identical outcomes using ValidationMessages constants
            Assert.That(asyncResult.IsValid, Is.EqualTo(syncResult.IsValid));
            Assert.That(asyncResult.ErrorCode, Is.EqualTo(syncResult.ErrorCode));
            Assert.That(asyncResult.ErrorMessage, Is.EqualTo(syncResult.ErrorMessage));

            //Verify expected ValidationMessages constants
            Assert.That(asyncResult.ErrorMessage, Is.EqualTo(ValidationMessages.CONSECUTIVE_SPACES_VALIDATION));
            Assert.That(asyncResult.ErrorCode, Is.EqualTo(ValidationMessages.CONSECUTIVE_SPACES_VALIDATION_ERROR_CODE));
        }
        #endregion
    }
}
