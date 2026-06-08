using Microsoft.Extensions.Configuration;

namespace AppTrust.Sdk;

public static class TrustOptionsConfiguration
{
    public static TrustOptions Bind(IConfiguration configuration)
    {
        var modeValue = configuration.GetSection(TrustOptions.SectionName)["Mode"] ?? "JWT";
        if (!TrustModeParser.TryParse(modeValue, out var mode))
        {
            throw new InvalidOperationException(
                $"Trust:Mode must be one of 'JWT', 'mTLS', or 'Both'. Got '{modeValue}'.");
        }

        return new TrustOptions { Mode = mode };
    }

    public static void Configure(TrustOptions options, IConfiguration configuration)
    {
        options.Mode = Bind(configuration).Mode;
    }
}
