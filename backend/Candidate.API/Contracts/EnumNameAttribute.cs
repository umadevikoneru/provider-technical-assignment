using System.ComponentModel.DataAnnotations;

namespace Candidate.API.Contracts;

/// <summary>
/// Validates that a string is the name of a value of <paramref name="enumType"/> (case-insensitive).
/// Taking the value as a string gives callers a normal field error ("Status must be one of ...")
/// instead of a JSON conversion failure for the whole body.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class EnumNameAttribute(Type enumType) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            // [Required] reports missing values.
            return ValidationResult.Success;
        }

        if (value is string text && Enum.TryParse(enumType, text.Trim(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(enumType, parsed))
        {
            return ValidationResult.Success;
        }

        var allowed = string.Join(", ", Enum.GetNames(enumType));
        return new ValidationResult(
            ErrorMessage ?? $"{validationContext.DisplayName} must be one of: {allowed}.",
            validationContext.MemberName is null ? null : [validationContext.MemberName]);
    }
}
