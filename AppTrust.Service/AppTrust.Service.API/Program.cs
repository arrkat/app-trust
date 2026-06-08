using AppTrust.Service.Contracts;
using AppTrust.Service.Infrastructure;
using AppTrust.Service.Infrastructure.Strategies;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Options;
using AppTrust.Sdk;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

var trustOptions = TrustOptionsConfiguration.Bind(builder.Configuration);
var trustMode = trustOptions.Mode;

var serviceOptions = builder.Configuration.GetSection(AppTrustServiceOptions.SectionName).Get<AppTrustServiceOptions>() ?? new AppTrustServiceOptions();
builder.Services.Configure<AppTrustServiceOptions>(builder.Configuration.GetSection(AppTrustServiceOptions.SectionName));
builder.Services.AddSingleton(trustOptions);
builder.Services.AddSingleton<IOptions<TrustOptions>>(new OptionsWrapper<TrustOptions>(trustOptions));

var kestrelConfiguredExternally = builder.Configuration.GetValue<bool>(AppTrustServiceOptions.KestrelConfiguredExternallyKey);
var requiresMutualTls = trustMode is TrustMode.Mtls or TrustMode.Both;

if (requiresMutualTls && !kestrelConfiguredExternally)
{
    builder.WebHost.UseUrls(builder.Configuration["AppTrustService:HttpsUrl"] ?? "https://localhost:7214");
}

X509Certificate2? serverCertificate = null;
if (requiresMutualTls && !kestrelConfiguredExternally)
{
    var serverCertificatePath = serviceOptions.ServerCertificatePath
        ?? throw new InvalidOperationException(
            $"AppTrustService:ServerCertificatePath is required when Trust:Mode is {TrustModeParser.ToWireString(trustMode)}.");

    serverCertificate = PemCertificateLoader.LoadCertificateWithPrivateKey(serverCertificatePath);
    var allowedSubjects = serviceOptions.AllowedClientCertificateSubjects;

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureEndpointDefaults(listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
        options.ConfigureHttpsDefaults(https =>
        {
            https.ServerCertificate = serverCertificate;
            https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            https.ClientCertificateValidation = (certificate, _, _) =>
                ClientCertificateRules.IsValidClientCertificate(certificate, allowedSubjects);
        });
    });
}

builder.Services.AddControllers();
builder.Services.AddSingleton<IKeyLoader>(_ => new FileKeyLoader(serviceOptions.PublicKeyPath));
builder.Services.AddSingleton<CachedRsaKeyLoader>();
builder.Services.AddSingleton<IJtiReplayCache, InMemoryJtiReplayCache>();
builder.Services.AddSingleton<JwtTokenValidator>();
builder.Services.AddSingleton<ITokenValidator>(sp => sp.GetRequiredService<JwtTokenValidator>());
builder.Services.AddSingleton<ClientCertificateValidator>();
builder.Services.AddSingleton<ICertificateValidator>(sp => sp.GetRequiredService<ClientCertificateValidator>());
builder.Services.AddSingleton<JwtInboundStrategy>();
builder.Services.AddSingleton<MtlsInboundStrategy>();
builder.Services.AddSingleton<InboundTrustStrategyHandler>(sp =>
    new InboundTrustStrategyHandler([
        sp.GetRequiredService<JwtInboundStrategy>(),
        sp.GetRequiredService<MtlsInboundStrategy>()
    ]));
builder.Services.AddSingleton<IInboundTrustStrategy>(sp =>
    trustMode switch
    {
        TrustMode.Mtls => sp.GetRequiredService<MtlsInboundStrategy>(),
        TrustMode.Both => sp.GetRequiredService<InboundTrustStrategyHandler>(),
        _              => sp.GetRequiredService<JwtInboundStrategy>()
    });
builder.Services.AddSingleton<ICorrelationIdGenerator, CorrelationIdGenerator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

try
{
    await app.Services.GetRequiredService<JwtTokenValidator>().WarmupAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "JWT key warmup failed at startup; will retry on first request.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", trustMode = TrustModeParser.ToWireString(trustMode) }));

app.MapControllers();

app.Run();
