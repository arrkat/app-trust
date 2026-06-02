using AppA.Contracts;
using AppA.Infrastructure;
using Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IKeyLoader>(_ => new FileKeyLoader("appA_private.pem"));
builder.Services.AddSingleton<AsymmetricJwtProvider>();
builder.Services.AddSingleton<ITokenProvider>(sp => sp.GetRequiredService<AsymmetricJwtProvider>());
builder.Services.AddHttpClient<AppBClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AppB:BaseUrl"] ?? "http://localhost:5278/");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

try
{
    await app.Services.GetRequiredService<AsymmetricJwtProvider>().WarmupAsync();
}
catch
{
    // Startup must remain resilient even if warmup throws unexpectedly.
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
