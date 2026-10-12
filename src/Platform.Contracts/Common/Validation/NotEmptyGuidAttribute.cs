using System.ComponentModel.DataAnnotations;

namespace Platform.Contracts.Common.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute()
        : base("The field {0} must be a non-empty GUID.")
    {
    }

    public override bool IsValid(object? value)
    {
        // Null means "not supplied" for optional Guid? fields; pair with [Required] to demand a value.
        return value is null || value is Guid guid && guid != Guid.Empty;
    }
}
