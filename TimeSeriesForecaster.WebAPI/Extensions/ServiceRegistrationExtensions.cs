using AutoMapper;
using Hangfire;
using Hangfire.PostgreSql;
using TimeSeriesForecaster.Application.Configuration;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Application.Services;
using TimeSeriesForecaster.Infrastructure.Persistence;
using TimeSeriesForecaster.Infrastructure.Repositories;

namespace TimeSeriesForecaster.WebAPI.Extensions;

public static class ServiceRegistrationExtensions
{
    // Application katmanındaki servisler (iş mantığı)
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IDatasetService, DatasetService>();
        services.AddScoped<IModelService, ModelService>();

        // Hangfire background job'ları tarafından çalıştırılan servisler.
        // Bunlar registered olmazsa Hangfire job'ı activate ederken (job'ın
        // gerçekten çalıştığı anda, Enqueue anında değil) hata fırlatır.
        services.AddScoped<IDataProcessingService, DataProcessingService>();
        services.AddScoped<IModelProcessingService, ModelProcessingService>();

        services.AddAutoMapper(typeof(TimeSeriesForecaster.Application.Mappings.MappingProfile));

        return services;
    }

    // Infrastructure katmanı: repository'ler, UnitOfWork
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IDatasetRepository, DatasetRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<IDataPointRepository, DataPointRepository>();
        services.AddScoped<IPredictionRepository, PredictionRepository>();
        services.AddScoped<IModelMetricRepository, ModelMetricRepository>();

        return services;
    }

    // Hangfire kurulumu
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));
        services.AddHangfireServer();

        return services;
    }

    // Dış servisler (ML servisi vs.) için tipli konfigürasyon + HttpClient kaydı
    public static IServiceCollection AddExternalServiceClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MlServiceSettings>(configuration.GetSection("MlService"));
        services.AddHttpClient();

        return services;
    }
}