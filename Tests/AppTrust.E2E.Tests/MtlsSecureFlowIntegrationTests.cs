using System.Net;
using System.Net.Http.Json;
using AppTrust.Client.Infrastructure;
using AppTrust.Service.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using AppTrust.Sdk;

namespace AppTrust.E2E.Tests;

public class MtlsSecureFlowIntegrationTests
{
    [Fact]
    public async Task TriggerSecureAction_MtlsOverHttps_ReturnsSuccess()
    {
        // Arrange — AppTrust.Service runs on real Kestrel HTTPS (client cert required); AppTrust.Client attaches its client cert
        using var mtls = CryptoTestCertificates.CreateOnDisk();

        await using var serviceFactory = new KestrelWebApplicationFactory<AppTrust.Service.API.Controllers.SecureActionController>(
            builder =>
            {
                builder.UseSetting($"{TrustOptions.SectionName}:Mode", "mTLS");
                builder.UseSetting($"{AppTrustServiceOptions.SectionName}:ServerCertificatePath", mtls.ServerPemPath);
            },
            serverCertificatePath: mtls.ServerPemPath);

        var serviceBaseUrl = serviceFactory.GetBaseAddress();

        await using var clientFactory = new WebApplicationFactory<AppTrust.Client.API.Controllers.ActionController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("AppTrustService:BaseUrl", serviceBaseUrl);
                builder.UseSetting($"{TrustOptions.SectionName}:Mode", "mTLS");
                builder.UseSetting($"{AppTrustClientOptions.SectionName}:ClientCertificatePath", mtls.ClientPemPath);
                builder.UseSetting($"{AppTrustClientOptions.SectionName}:TriggerApiKey", IntegrationTestHelpers.TriggerApiKey);
                builder.UseSetting($"{AppTrustClientOptions.SectionName}:AcceptAnyServerCertificate", "true");
            });

        var clientHttpClient = clientFactory.CreateClient();

        // Act
        var response = await clientHttpClient.SendAsync(IntegrationTestHelpers.CreateTriggerRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<SecureActionResponse>();
        Assert.NotNull(content);
        Assert.Equal("Secure action executed successfully!", content.Message);
        Assert.Equal(AppConstants.ClientIdentifier, content.Caller);
        Assert.NotEqual(Guid.Empty, content.CorrelationId);
    }

    private sealed record SecureActionResponse(string Message, string Caller, Guid CorrelationId);
}
