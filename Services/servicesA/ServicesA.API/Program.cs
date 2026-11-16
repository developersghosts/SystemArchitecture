var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogLogging();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Swagger UI at /swagger, reading the built-in OpenAPI document
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ServicesA API v1");
        options.DocumentTitle = "ServicesA API";
        options.EnablePersistAuthorization();
    });
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors(ServicesA.API.ServiceInjection.UiCorsPolicy);

app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapCarter();

app.MapGet("/", () => "ServicesA is running");

app.Run();
