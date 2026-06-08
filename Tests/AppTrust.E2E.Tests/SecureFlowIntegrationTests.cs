using System.Net;
using System.Net.Http.Json;
using AppTrust.Client.Infrastructure;
using AppTrust.Service.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using AppTrust.Sdk;

namespace AppTrust.E2E.Tests;

public class SecureFlowIntegrationTests
{
    [Fact]
    public async Task TriggerSecureAction_EndToEndFlow_ReturnsSuccessFromAppTrustService()
    {
        // Arrange — spin up AppTrust.Service on TestServer, then wire AppTrust.Client's HttpClient to route through it
        using var keys = CryptoTestKeys.CreateKeyPairOnDisk();

        await using var serviceFactory = new WebApplicationFactory<AppTrust.Service.API.Controllers.SecureActionController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting($"{TrustOptions.SectionName}:Mode", "JWT");
                builder.UseSetting($"{AppTrustServiceOptions.SectionName}:PublicKeyPath", keys.PublicKeyPath);
            });

        _ = serviceFactory.Server;
        var serviceHandler = serviceFactory.Server.CreateHandler();
        var serviceBaseAddress = serviceFactory.Server.BaseAddress
            ?? throw new InvalidOperationException("AppTrust.Service test server has no base address.");

        await using var clientFactory = new WebApplicationFactory<AppTrust.Client.API.Controllers.ActionController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting($"{TrustOptions.SectionName}:Mode", "JWT");
                builder.UseSetting("AppTrustService:BaseUrl", "https://localhost/");
                builder.UseSetting($"{AppTrustClientOptions.SectionName}:PrivateKeyPath", keys.PrivateKeyPath);
                builder.UseSetting($"{AppTrustClientOptions.SectionName}:TriggerApiKey", IntegrationTestHelpers.TriggerApiKey);
                builder.ConfigureTestServices(services =>
                {
                    services.AddHttpClient<AppTrustServiceClient>()
                        .ConfigurePrimaryHttpMessageHandler(_ => serviceHandler)
                        .ConfigureHttpClient(client => client.BaseAddress = serviceBaseAddress);
                });
            });

        var clientHttpClient = clientFactory.CreateClient();

        await clientFactory.Services.GetRequiredService<AsymmetricJwtProvider>().WarmupAsync();
        await serviceFactory.Services.GetRequiredService<JwtTokenValidator>().WarmupAsync();

        // Act — trigger AppTrust.Client, which signs a JWT and calls AppTrust.Service
        var response = await clientHttpClient.SendAsync(IntegrationTestHelpers.CreateTriggerRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<SecureActionResponse>();
        Assert.NotNull(content);
        Assert.Equal("Secure action executed successfully!", content.Message);
        Assert.Equal(AppConstants.ClientIdentifier, content.Caller);
        Assert.NotEqual(Guid.Empty, content.CorrelationId);

        // Act — second trigger should produce a new correlation ID (stateless)
        var secondResponse = await clientHttpClient.SendAsync(IntegrationTestHelpers.CreateTriggerRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var secondContent = await secondResponse.Content.ReadFromJsonAsync<SecureActionResponse>();
        Assert.NotNull(secondContent);
        Assert.NotEqual(Guid.Empty, secondContent.CorrelationId);
        Assert.NotEqual(content.CorrelationId, secondContent.CorrelationId);
    }

    private sealed record SecureActionResponse(string Message, string Caller, Guid CorrelationId);
}
