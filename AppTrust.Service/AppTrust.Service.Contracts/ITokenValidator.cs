
namespace AppTrust.Service.Contracts;

public interface ITokenValidator
{
    Task<TokenValidationResult> ValidateTokenAsync(
        string token,
        string? presentedCertificateThumbprint = null);
}
