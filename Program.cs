var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Protect Swagger/OpenAPI when running in Cloud Run by requiring a password-only
// Basic auth. The middleware is active only when the Cloud Run environment
// variable `K_SERVICE` is present. Password is read from `SWAGGER_AUTH_PASSWORD`.
var isCloudRun = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("K_SERVICE"));
app.Use(async (context, next) =>
{
    if (!isCloudRun)
    {
        await next();
        return;
    }

    var path = context.Request.Path.Value ?? string.Empty;
    if (!path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    var pwEnv = Environment.GetEnvironmentVariable("SWAGGER_AUTH_PASSWORD");
    if (string.IsNullOrEmpty(pwEnv))
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Swagger auth not configured.");
        return;
    }

    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Swagger\"";
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    string encoded = authHeader.Substring("Basic ".Length).Trim();
    string decoded;
    try
    {
        decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
    }
    catch
    {
        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Swagger\"";
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    // decoded is "username:password" or ":password". We only validate the password.
    var idx = decoded.IndexOf(':');
    string password = idx >= 0 ? decoded.Substring(idx + 1) : decoded ?? string.Empty;

    var pwBytes = System.Text.Encoding.UTF8.GetBytes(password);
    var envBytes = System.Text.Encoding.UTF8.GetBytes(pwEnv);
    var isMatch = pwBytes.Length == envBytes.Length && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(pwBytes, envBytes);

    if (!isMatch)
    {
        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Swagger\"";
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    await next();
});

app.MapOpenApi();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
