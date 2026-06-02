
namespace AppB.Contracts;

public interface ITokenValidator
{
    Task<TokenValidationResult> ValidateTokenAsync(string token);
}
