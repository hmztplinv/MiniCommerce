namespace MiniCommerce.Shared.Common;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new("None", string.Empty);

    public static Error Validation(string message = "One or more validation errors occurred.")
        => new("Validation.Error", message);
}
