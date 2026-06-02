namespace AppB.Contracts;

public record SecureActionResult(string Message, string Caller, Guid SessionId);
