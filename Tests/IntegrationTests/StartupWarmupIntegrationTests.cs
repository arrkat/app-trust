using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using AppA.Contracts;
using AppA.Infrastructure;
using Shared.Infrastructure;

namespace IntegrationTests;

public class StartupWarmupIntegrationTests
{
    [Fact]
    public async Task ProgramStartup_WhenKeyLoaderFails_ShouldStillBuildAndRespond()
    {
        await using var appAFactory = new WebApplicationFactory<AppA.API.Controllers.ActionController>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IKeyLoader, BrokenKeyLoader>();
                    services.AddSingleton<AsymmetricJwtProvider>();
                    services.AddSingleton<ITokenProvider, AsymmetricJwtProvider>();
                    services.AddSingleton(new HttpClient());
                    services.AddScoped<AppBClient>();
                });
            });

        var httpClient = appAFactory.CreateClient();

        var response = await httpClient.PostAsync("api/action/trigger", null);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private sealed class BrokenKeyLoader : IKeyLoader
    {
        public Task<string> LoadKeyPemAsync() =>
            throw new FileNotFoundException("CRITICAL: Key file is missing from disk!");
    }
}
