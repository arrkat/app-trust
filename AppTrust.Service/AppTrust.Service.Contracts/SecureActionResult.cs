namespace AppTrust.Service.Contracts;

public record SecureActionResult(string Message, string Caller, Guid CorrelationId);
