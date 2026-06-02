namespace Shared.Infrastructure;

public interface IKeyLoader
{
    Task<string> LoadKeyPemAsync();
}
