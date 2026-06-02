
namespace AppA.Contracts;

public interface ITokenProvider
{
    Task<string> GetTokenAsync();
}
