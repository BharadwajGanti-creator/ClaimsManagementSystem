using Learning_Project.Models;

namespace Learning_Project.Interfaces
{
    //This interface defines the contract for name validation services.
    public interface INameValidationService
    {
        public ValidationResult ValidateName(string name);
    }
}
