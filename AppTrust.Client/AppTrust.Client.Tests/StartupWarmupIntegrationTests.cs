using System.Net;
using AppTrust.Client.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using AppTrust.Sdk;

namespace AppTrust.Client.Tests;

public class StartupWarmupIntegrationTests
{
    [Fact]
    public async Task ProgramStartup_WhenKeyLoaderFails_ShouldStillBuildAndRespond()
    {
        // Arrange — inject a broken key loader; startup must not crash, but the trigger call fails at runtime
        await using var clientFactory = new WebApplicationFactory<AppTrust.Client.API.Controllers.ActionController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting($"{TrustOptions.SectionName}:Mode", "JWT");
                builder.UseSetting($"{AppTrustClientOptions.SectionName}:TriggerApiKey", IntegrationTestHelpers.TriggerApiKey);
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IKeyLoader, BrokenKeyLoader>();
                });
            });

        var httpClient = clientFactory.CreateClient();

        // Act
        var response = await httpClient.SendAsync(IntegrationTestHelpers.CreateTriggerRequest());

        // Assert — host is alive; JWT signing fails when the action tries to call AppTrust.Service
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private sealed class BrokenKeyLoader : IKeyLoader
    {
        public Task<string> LoadKeyPemAsync() =>
            throw new FileNotFoundException("CRITICAL: Key file is missing from disk!");
    }
}
