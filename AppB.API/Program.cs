using AppB.Contracts;
using AppB.Infrastructure;
using Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IKeyLoader>(_ => new FileKeyLoader("appA_public.pem"));
builder.Services.AddSingleton<JwtTokenValidator>();
builder.Services.AddSingleton<ITokenValidator>(sp => sp.GetRequiredService<JwtTokenValidator>());
builder.Services.AddSingleton<ISessionService, InMemorySessionService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

try
{
    await app.Services.GetRequiredService<JwtTokenValidator>().WarmupAsync();
}
catch
{
    // Startup must remain resilient even if warmup throws unexpectedly.
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
