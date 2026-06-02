namespace Shared.Infrastructure;

public class FileKeyLoader : IKeyLoader
{
    private readonly string _keyFilePath;

    public FileKeyLoader(string keyFilePath)
    {
        _keyFilePath = keyFilePath;
    }

    public Task<string> LoadKeyPemAsync()
    {
        if (!File.Exists(_keyFilePath))
            throw new FileNotFoundException($"Key file missing at: {_keyFilePath}");

        return Task.FromResult(File.ReadAllText(_keyFilePath));
    }
}
