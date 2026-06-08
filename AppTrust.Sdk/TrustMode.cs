namespace AppTrust.Sdk;



public enum TrustMode

{

    Jwt,

    Mtls,

    /// <summary>
    /// Requires every registered inbound strategy to succeed with the same CallerId
    /// (e.g. a valid JWT and a valid client certificate on the same request).
    /// </summary>
    Both

}



public static class TrustModeParser

{

    public static bool TryParse(string? value, out TrustMode mode)

    {

        switch (value)

        {

            case "JWT":

                mode = TrustMode.Jwt;

                return true;

            case "mTLS":

                mode = TrustMode.Mtls;

                return true;

            case "Both":

                mode = TrustMode.Both;

                return true;

            default:

                mode = default;

                return false;

        }

    }



    public static TrustMode Parse(string? value)

    {

        if (!TryParse(value, out var mode))

        {

            throw new InvalidOperationException(

                $"Unknown trust mode: '{value}'. Valid values are 'JWT', 'mTLS', 'Both'.");

        }



        return mode;

    }



    /// <summary>

    /// Returns the canonical wire/config string for a TrustMode value.

    /// These strings must match what Trust:Mode uses in appsettings.json.

    /// </summary>

    public static string ToWireString(TrustMode mode) =>

        mode switch

        {

            TrustMode.Mtls => "mTLS",

            TrustMode.Both => "Both",

            _              => "JWT"

        };

}

