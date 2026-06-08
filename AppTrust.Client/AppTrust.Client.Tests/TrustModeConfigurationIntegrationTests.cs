using AppTrust.Client.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using AppTrust.Sdk;

namespace AppTrust.Client.Tests;

public class TrustModeConfigurationIntegrationTests
{
    [Fact]
    public void AppTrustClientStartup_WhenTrustModeIsInvalid_RefusesToStart()
    {
        // Arrange & Act — accessing factory.Server forces host startup; invalid Trust:Mode must fail fast
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var factory = new WebApplicationFactory<AppTrust.Client.API.Controllers.ActionController>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting($"{TrustOptions.SectionName}:Mode", "UnknownMode");
                    builder.UseSetting($"{AppTrustClientOptions.SectionName}:TriggerApiKey", IntegrationTestHelpers.TriggerApiKey);
                });

            _ = factory.Server;
        });

        // Assert
        Assert.Contains("Trust:Mode", exception.Message);
    }
}
