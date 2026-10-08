using Core.Cache.Options;
using Core.Cache.Rehydration.Abstractions;
using Core.Cache.Rehydration.Background;
using Core.Cache.Rehydration.Memory;
using Core.Cache.Rehydration.Options;
using Core.Cache.Rehydration.Primary;
using Core.Cache.Rehydration.Services;
using Core.Cache.Storage.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Cache.Rehydration.DependencyInjection;

/// <summary>
/// Registers the cache rehydration services.
/// </summary>
public static class RehydrationRegistration
{
    /// <summary>
    /// Registers the rehydration source, target, coordinator, service and the
    /// hosted background service that drives the cycles.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Callback that configures the rehydration options.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="configure"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <c>AddCoreCache()</c> has not been called, or no
    /// <c>IExternalCacheStorage</c> is registered.
    /// </exception>
    /// <remarks>
    /// Requires <c>AddHealthChecks()</c>: the rehydration service takes a
    /// <c>HealthCheckService</c>, so the host fails to start without it. When the
    /// core cache or rehydration is disabled, nothing is registered and no
    /// exception is thrown.
    /// </remarks>
    public static IServiceCollection AddCoreCacheRehydration(
        this IServiceCollection services,
        Action<RehydrationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var cacheOptions = GetCacheOptions(services);

        if (!cacheOptions.Enabled)
        {
            return services;
        }

        EnsurePrimaryRegistered(services);

        var rehydrationOptions = BuildOptions(configure);

        services.AddSingleton(rehydrationOptions);

        if (!rehydrationOptions.Enabled)
            return services;

        services.AddSingleton<IRehydrationSource,
            MemoryRehydrationSource>();

        services.AddSingleton<IRehydrationTarget,
            PrimaryRehydrationTarget>();

        services.AddSingleton<ICacheRehydrator,
            CacheRehydrator>();

        services.AddSingleton<IRehydrationService,
            RehydrationService>();

        services.AddHostedService<
            RehydrationBackgroundService>();

        return services;
    }

    private static CacheOptions GetCacheOptions(
    IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(
            x => x.ServiceType == typeof(CacheOptions));

        if (descriptor?.ImplementationInstance is not CacheOptions options)
        {
            throw new InvalidOperationException(
                RehydrationMessages.CacheRegistrationRequired);
        }

        return options;
    }

    private static RehydrationOptions BuildOptions(
        Action<RehydrationOptions> configure)
    {
        var options = new RehydrationOptions();

        configure(options);

        return options;
    }

    private static void EnsurePrimaryRegistered(
    IServiceCollection services)
    {
        if (services.All(
            d => d.ServiceType != typeof(IExternalCacheStorage)))
        {
            throw new InvalidOperationException(
                RehydrationMessages.PrimaryRegistrationRequired);
        }
    }
}