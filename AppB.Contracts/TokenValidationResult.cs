namespace AppB.Contracts;

public record TokenValidationResult(bool IsValid, string? CallerId);
