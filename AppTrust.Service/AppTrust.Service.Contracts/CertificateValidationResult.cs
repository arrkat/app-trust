namespace AppTrust.Service.Contracts;

public record CertificateValidationResult(bool IsValid, string? CallerId);
