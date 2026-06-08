namespace AppTrust.Sdk;

public class FileKeyLoader : IKeyLoader
{
    private readonly string _keyFilePath;

    public FileKeyLoader(string keyFilePath)
    {
        _keyFilePath = keyFilePath;
    }

    public async Task<string> LoadKeyPemAsync()
    {
        if (!File.Exists(_keyFilePath))
            throw new FileNotFoundException($"Key file missing at: {_keyFilePath}");

        return await File.ReadAllTextAsync(_keyFilePath);
    }
}
