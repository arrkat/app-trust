using System.Net;
using System.Net.Http.Json;
using AppTrust.Client.Infrastructure;
using AppTrust.Service.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using AppTrust.Sdk;

namespace AppTrust.E2E.Tests;

public class BothSecureFlowIntegrationTests
{
    [Fact]
    public async Task TriggerSecureAction_BothOverHttps_BindsJwtToClientCertificate()
    {
        // Arrange — Both mode: JWT must carry the client cert thumbprint (x5t#S256) and mTLS must present the cert
        using var keys = CryptoTestKeys.CreateKeyPairOnDisk();
        using var mtls = CryptoTestCertificates.CreateOnDisk();

        await using var serviceFactory = new KestrelWebApplicationFactory<AppTrust.Service.API.Controllers.SecureActionController>(
            builder =>
            {
                builder.UseSetting($"{TrustOptions.SectionName}:Mode", "Both");
                builder.UseSetting($"{AppTrustServiceOptions.SectionName}:ServerCertificatePath", mtls.ServerPemPath);
                builder.UseSetting($"{AppTrustServiceOptions.SectionName}:PublicKeyPath", keys.PublicKeyPath);
            },
            serverCertificatePath: mtls.ServerPemPath);

        var serviceBaseUrl = serviceFactory.GetBaseAddress();

        await using var clientFactory = new WebApplicationFactory<AppTrust.Client.API.Controllers.ActionController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("AppTrustService:BaseUrl", serviceBaseUrl);
                builder.UseSetting($"{TrustOptions.SectionName}:Mode", "Both");
                builder.UseSetting($"{AppTrustClientOptions.SectionName}:ClientCertificatePath", mtls.ClientPemPath);
                builder.UseSetting($"{AppTrustClientOptions.SectionName}:PrivateKeyPath", keys.PrivateKeyPath);
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
