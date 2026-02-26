using System.ComponentModel.DataAnnotations;

namespace LostAndFoundApp.Models
{
    /// <summary>
    /// Validation attribute that ensures a DateTime value is not in the future.
    /// Used on date fields where a future date is logically invalid (e.g., DateFound).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NotFutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime dateValue)
            {
                if (dateValue.Date > DateTime.Today)
                {
                    return new ValidationResult(
                        ErrorMessage ?? $"{validationContext.DisplayName} cannot be in the future.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
