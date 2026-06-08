namespace AppTrust.Service.Contracts;

public record TokenValidationResult(bool IsValid, string? CallerId);
