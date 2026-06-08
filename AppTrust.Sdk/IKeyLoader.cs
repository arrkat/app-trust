namespace AppTrust.Sdk;

public interface IKeyLoader
{
    Task<string> LoadKeyPemAsync();
}
