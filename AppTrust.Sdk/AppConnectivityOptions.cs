namespace AppTrust.Sdk;

public static class AppConnectivityOptions
{
    public static void ValidateAppTrustServiceBaseUrlForTrustMode(string serviceBaseUrl, TrustMode trustMode)
    {
        if (!serviceBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"AppTrustService:BaseUrl must use https (trust mode is '{TrustModeParser.ToWireString(trustMode)}'). Got '{serviceBaseUrl}'.");
        }
    }
}
