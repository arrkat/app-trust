using AppTrust.Client.Contracts;
using AppTrust.Client.Infrastructure;
using AppTrust.Client.Infrastructure.Strategies;
using AppTrust.Sdk;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

var clientOptions = BindAppTrustClientOptions(builder.Configuration);
var trustOptions = TrustOptionsConfiguration.Bind(builder.Configuration);
var trustMode = trustOptions.Mode;

builder.Services.Configure<AppTrustClientOptions>(options =>
{
    options.ClientCertificatePath = clientOptions.ClientCertificatePath;
    options.PrivateKeyPath = clientOptions.PrivateKeyPath;
    options.CallerId = clientOptions.CallerId;
    options.TriggerApiKey = clientOptions.TriggerApiKey;
    options.AcceptAnyServerCertificate = clientOptions.AcceptAnyServerCertificate;
});
builder.Services.AddSingleton(trustOptions);

var serviceBaseUrl = builder.Configuration["AppTrustService:BaseUrl"] ?? "https://localhost:7214/";
var clientCertificatePath = clientOptions.ClientCertificatePath;

if (clientOptions.AcceptAnyServerCertificate && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "AppTrustClient:AcceptAnyServerCertificate is only permitted in Development.");
}

var acceptAnyServerCertificate = builder.Environment.IsDevelopment()
    && clientOptions.AcceptAnyServerCertificate;

AppConnectivityOptions.ValidateAppTrustServiceBaseUrlForTrustMode(serviceBaseUrl, trustMode);

var certificateLoader = new FileCertificateLoader(clientCertificatePath);
X509Certificate2? mtlsClientCertificate = null;
if (trustMode is TrustMode.Mtls or TrustMode.Both)
    mtlsClientCertificate = await certificateLoader.LoadCertificateAsync();

builder.Services.AddControllers();
builder.Services.AddSingleton(clientOptions);
builder.Services.AddSingleton<IKeyLoader>(_ => new FileKeyLoader(clientOptions.PrivateKeyPath));
builder.Services.AddSingleton<CachedRsaKeyLoader>();
builder.Services.AddSingleton(certificateLoader);
builder.Services.AddSingleton<ICertificateLoader>(sp => sp.GetRequiredService<FileCertificateLoader>());
builder.Services.AddSingleton<AsymmetricJwtProvider>();
builder.Services.AddSingleton<ITokenProvider>(sp => sp.GetRequiredService<AsymmetricJwtProvider>());
builder.Services.AddSingleton<JwtOutboundStrategy>(sp => new JwtOutboundStrategy(
    sp.GetRequiredService<ITokenProvider>(),
    bindCertificateThumbprint: trustMode == TrustMode.Both,
    certificateLoader: trustMode == TrustMode.Both
        ? sp.GetRequiredService<ICertificateLoader>()
        : null));
builder.Services.AddSingleton<NoOpOutboundStrategy>();
builder.Services.AddSingleton<IOutboundTrustStrategy>(sp =>
    trustMode switch
    {
        TrustMode.Mtls => sp.GetRequiredService<NoOpOutboundStrategy>(),
        _              => sp.GetRequiredService<JwtOutboundStrategy>()
    });
builder.Services.AddHttpClient<AppTrustServiceClient>(client =>
{
    client.BaseAddress = new Uri(serviceBaseUrl);
})
.ConfigurePrimaryHttpMessageHandler(_ =>
    AppTrustServiceHttpClientHandlerFactory.Create(mtlsClientCertificate, acceptAnyServerCertificate));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

try
{
    await app.Services.GetRequiredService<AsymmetricJwtProvider>().WarmupAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "JWT key warmup failed at startup; will retry on first request.");
}

app.UseMiddleware<TriggerApiKeyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", trustMode = TrustModeParser.ToWireString(trustMode) }));

app.MapControllers();

app.Run();

static AppTrustClientOptions BindAppTrustClientOptions(IConfiguration configuration) =>
    configuration.GetSection(AppTrustClientOptions.SectionName).Get<AppTrustClientOptions>() ?? new AppTrustClientOptions();
