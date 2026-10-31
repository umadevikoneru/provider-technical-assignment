using System.Text.Json.Serialization;
using Candidate.API.Data;
using Candidate.API.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
    {
        // Report validation errors under the JSON names the client sent (providerName, not ProviderName).
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
        // Expected business/database failures become 4xx ProblemDetails; see ApiExceptionFilter.
        options.Filters.Add<ApiExceptionFilter>();
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlDocs = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlDocs))
    {
        options.IncludeXmlComments(xmlDocs);
    }
});

// Unexpected exceptions: UseExceptionHandler logs them and returns a 500 ProblemDetails.
builder.Services.AddProblemDetails();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<AuditStampInterceptor>();

// The connection string is resolved when the context is created (not here) so tests can override it.
builder.Services.AddDbContext<AppDbContext>((services, options) =>
{
    var connectionString = DatabaseInitializer.ResolveConnectionString(
        services.GetRequiredService<IConfiguration>(),
        services.GetRequiredService<IHostEnvironment>());

    options.UseSqlite(connectionString)
        .AddInterceptors(services.GetRequiredService<AuditStampInterceptor>())
        // EF logs a cancelled connection open (the client aborted the request, e.g. a superseded
        // search) as an error. Real connection failures still fail the request and are logged as
        // errors by UseExceptionHandler, so this event alone is noise.
        .ConfigureWarnings(warnings => warnings.Log((RelationalEventId.ConnectionError, LogLevel.Debug)));
});

builder.Services.AddApplicationServices();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

DatabaseInitializer.EnsureCreated(
    DatabaseInitializer.ResolveConnectionString(app.Configuration, app.Environment),
    DatabaseInitializer.ScriptsDirectory(app.Environment),
    app.Logger);

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // The API has no home page; send anyone who opens its base address to the API docs.
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}
else
{
    // In Development the React app reaches the API through the Vite proxy over plain HTTP;
    // redirecting those calls to the HTTPS port would turn them into cross-origin requests.
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

// Liveness/readiness probe for monitoring: 200 "Healthy" when the database is reachable, 503 otherwise.
app.MapHealthChecks("/health");

app.Run();

// Lets the integration tests host the API with WebApplicationFactory<Program>.
public partial class Program;
