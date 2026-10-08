using Core.Cache.Abstractions;
using Core.Cache.Http;
using Core.Cache.Options;
using Core.Cache.Pipeline.Behaviors;
using Core.Cache.Services;
using Core.Serialization.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Cache.DependencyInjection;

/// <summary>
/// Registers the core cache services.
/// </summary>
public static class CacheRegistration
{
    /// <summary>
    /// Registers the cache pipeline, storage resolution and metrics.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Callback that configures the cache options.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddCoreCache(
        this IServiceCollection services,
        Action<CacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new CacheOptions();
        configure(options);

        services.AddSingleton(options);

        if (!options.Enabled)
        {
            services.AddSingleton<ICoreCache, NoOpCoreCache>();
            return services;
        }

        services
            .AddLogging()
            .AddCoreSerialization(serialization =>
            {
                serialization.DefaultSerializer = options.SerializerType;
            })
            .AddCacheDiagnostics()
            .AddCacheMemory();

        services
            .AddSingleton<LoggingBehavior>()
            .AddSingleton<MetricsBehavior>()
            .AddCachePipeline()
            .AddCacheHttp()
            .AddCacheServices();

        return services;
    }

    /// <summary>
    /// Adds the cache middleware to the request pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder, for chaining.</returns>
    /// <remarks>
    /// Only endpoints decorated with <c>CacheableAttribute</c> are cached.
    /// </remarks>
    public static IApplicationBuilder UseCoreCache(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<CacheOptions>()
            ?? throw new InvalidOperationException(
                CacheMessages.MissingRegistration);

        if (!options.Enabled)
        {
            return app;
        }

        if (app.ApplicationServices.GetService<IHttpCacheHandler>() is null)
        {
            throw new InvalidOperationException(CacheMessages.MissingRegistration);
        }

        return app.UseMiddleware<CacheMiddleware>();
    }
}