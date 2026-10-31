using Candidate.API.Services.Providers;
using Candidate.API.Services.Reports;

namespace Candidate.API.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers each use-case service behind its interface; consumers depend only on the interfaces.</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProviderQueryService, ProviderQueryService>();
        services.AddScoped<IProviderCommandService, ProviderCommandService>();
        services.AddScoped<IProviderAuditService, ProviderAuditService>();
        services.AddScoped<IScenarioReportService, ScenarioReportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
