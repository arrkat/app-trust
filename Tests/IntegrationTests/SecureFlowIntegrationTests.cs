using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using AppA.Contracts;
using AppA.Infrastructure;
using AppB.Contracts;
using AppB.Infrastructure;
using Shared.Infrastructure;

namespace IntegrationTests;

public class SecureFlowIntegrationTests
{
    [Fact]
    public async Task TriggerSecureAction_EndToEndFlow_ReturnsSuccessFromAppB()
    {
        using var keys = CryptoTestKeys.CreateKeyPairOnDisk();

        await using var appBFactory = new WebApplicationFactory<AppB.API.Controllers.SecureActionController>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IKeyLoader>(
                        new FileKeyLoader(keys.PublicKeyPath));
                    services.AddSingleton<JwtTokenValidator>();
                    services.AddSingleton<ITokenValidator, JwtTokenValidator>();
                    services.AddSingleton<ISessionService, InMemorySessionService>();
                });
            });

        var appBClient = appBFactory.CreateClient();

        await using var appAFactory = new WebApplicationFactory<AppA.API.Controllers.ActionController>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IKeyLoader>(
                        new FileKeyLoader(keys.PrivateKeyPath));
                    services.AddSingleton<AsymmetricJwtProvider>();
                    services.AddSingleton<ITokenProvider, AsymmetricJwtProvider>();
                    services.AddSingleton(appBClient);
                    services.AddScoped<AppBClient>();
                });
            });

        var appAHttpClient = appAFactory.CreateClient();

        await appAFactory.Services.GetRequiredService<AsymmetricJwtProvider>().WarmupAsync();
        await appBFactory.Services.GetRequiredService<JwtTokenValidator>().WarmupAsync();

        var response = await appAHttpClient.PostAsync("api/action/trigger", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<SecureActionResult>();
        Assert.NotNull(content);
        Assert.Equal("Secure action executed successfully!", content.Message);
        Assert.Equal(AppConstants.AppAIdentifier, content.Caller);
        Assert.NotEqual(Guid.Empty, content.SessionId);

        var secondResponse = await appAHttpClient.PostAsync("api/action/trigger", null);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var secondContent = await secondResponse.Content.ReadFromJsonAsync<SecureActionResult>();
        Assert.NotNull(secondContent);
        Assert.NotEqual(Guid.Empty, secondContent.SessionId);
        Assert.NotEqual(content.SessionId, secondContent.SessionId);
    }
}
