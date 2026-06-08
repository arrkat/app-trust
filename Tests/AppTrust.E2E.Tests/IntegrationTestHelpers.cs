using AppTrust.Sdk;

namespace AppTrust.E2E.Tests;

internal static class IntegrationTestHelpers
{
    public const string TriggerApiKey = "integration-test-trigger-key";

    public static HttpRequestMessage CreateTriggerRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/action/trigger");
        request.Headers.Add(AppConstants.TriggerApiKeyHeaderName, TriggerApiKey);
        return request;
    }
}
