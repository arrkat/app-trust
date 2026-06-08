using System.Security.Cryptography;

namespace AppTrust.E2E.Tests;

internal static class CryptoTestKeys
{
    public static TempKeyPair CreateKeyPairOnDisk()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();

        var privateKeyPath = Path.Combine(Path.GetTempPath(), $"apptrust-private-{Guid.NewGuid():N}.pem");
        var publicKeyPath = Path.Combine(Path.GetTempPath(), $"apptrust-public-{Guid.NewGuid():N}.pem");

        File.WriteAllText(privateKeyPath, privatePem);
        File.WriteAllText(publicKeyPath, publicPem);

        return new TempKeyPair(privateKeyPath, publicKeyPath);
    }
}

internal sealed class TempKeyPair : IDisposable
{
    public string PrivateKeyPath { get; }
    public string PublicKeyPath { get; }

    public TempKeyPair(string privateKeyPath, string publicKeyPath)
    {
        PrivateKeyPath = privateKeyPath;
        PublicKeyPath = publicKeyPath;
    }

    public void Dispose()
    {
        if (File.Exists(PrivateKeyPath))
            File.Delete(PrivateKeyPath);

        if (File.Exists(PublicKeyPath))
            File.Delete(PublicKeyPath);
    }
}
