using System.Security.Cryptography.X509Certificates;

namespace AppTrust.Sdk;

public static class PemCertificateLoader
{
    public static async Task<X509Certificate2> LoadCertificateWithPrivateKeyAsync(string certificateFilePath)
    {
        if (!File.Exists(certificateFilePath))
            throw new FileNotFoundException($"Certificate file missing at: {certificateFilePath}");

        var pem = await File.ReadAllTextAsync(certificateFilePath);
        return LoadFromCombinedPem(pem);
    }

    public static X509Certificate2 LoadCertificateWithPrivateKey(string certificateFilePath)
    {
        if (!File.Exists(certificateFilePath))
            throw new FileNotFoundException($"Certificate file missing at: {certificateFilePath}");

        var pem = File.ReadAllText(certificateFilePath);
        return LoadFromCombinedPem(pem);
    }

    internal static X509Certificate2 LoadFromCombinedPem(string pem)
    {
        var certPem = ExtractPemBlock(pem, "CERTIFICATE")
            ?? throw new InvalidOperationException("PEM file does not contain a certificate block.");

        var keyPem = ExtractPemBlock(pem, "PRIVATE KEY")
            ?? ExtractPemBlock(pem, "RSA PRIVATE KEY")
            ?? ExtractPemBlock(pem, "EC PRIVATE KEY");

        return keyPem is null
            ? X509Certificate2.CreateFromPem(certPem)
            : X509Certificate2.CreateFromPem(certPem, keyPem);
    }

    private static string? ExtractPemBlock(string pem, string label)
    {
        var header = $"-----BEGIN {label}-----";
        var footer = $"-----END {label}-----";
        var start = pem.IndexOf(header, StringComparison.Ordinal);
        if (start < 0)
            return null;

        var end = pem.IndexOf(footer, start, StringComparison.Ordinal);
        if (end < 0)
            return null;

        end += footer.Length;
        return pem[start..end];
    }
}
