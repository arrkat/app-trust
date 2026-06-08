using System.Net;
using System.Security.Cryptography.X509Certificates;
using AppTrust.Service.Infrastructure;
using AppTrust.Sdk;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppTrust.E2E.Tests;

internal sealed class KestrelWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    private readonly Action<IWebHostBuilder>? _configureWebHost;
    private readonly string? _serverCertificatePath;
    private IHost? _kestrelHost;
    private string? _resolvedBaseUrl;

    public KestrelWebApplicationFactory(
        Action<IWebHostBuilder>? configureWebHost = null,
        string? serverCertificatePath = null)
    {
        _configureWebHost = configureWebHost;
        _serverCertificatePath = serverCertificatePath;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting(AppTrustServiceOptions.KestrelConfiguredExternallyKey, "true");
        _configureWebHost?.Invoke(builder);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel(options =>
            {
                if (_serverCertificatePath is not null)
                {
                    var serverCertificate = PemCertificateLoader.LoadCertificateWithPrivateKey(_serverCertificatePath);
                    var allowedSubjects = new[] { AppConstants.ClientIdentifier };

                    options.Listen(IPAddress.Loopback, port: 0, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1;
                        listenOptions.UseHttps(https =>
                        {
                            https.ServerCertificate = serverCertificate;
                            https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                            https.ClientCertificateValidation = (certificate, _, _) =>
                                ClientCertificateRules.IsValidClientCertificate(certificate, allowedSubjects);
                        });
                    });
                }
                else
                {
                    options.Listen(IPAddress.Loopback, port: 0);
                }
            });
        });

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var addressesFeature = _kestrelHost.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("IServerAddressesFeature is unavailable.");

        var boundAddress = addressesFeature.Addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("Kestrel did not bind to any address.");

        _resolvedBaseUrl = boundAddress.TrimEnd('/') + '/';

        testHost.Start();
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _kestrelHost is not null)
        {
            _kestrelHost.StopAsync().GetAwaiter().GetResult();
            _kestrelHost.Dispose();
            _kestrelHost = null;
        }

        base.Dispose(disposing);
    }

    public string GetBaseAddress()
    {
        _ = Server;
        return _resolvedBaseUrl ?? throw new InvalidOperationException("Host has not started yet.");
    }
}
