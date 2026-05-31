using Gepa.Net.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Configure GEPA client
builder.Services.Configure<GepaClientOptions>(
    builder.Configuration.GetSection("Gepa"));

builder.Services.AddHttpClient<IGepaClient, GepaClient>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
