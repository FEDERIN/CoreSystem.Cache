using Core.Cache.Abstractions;
using Core.Cache.DependencyInjection;
using Core.Cache.Options;
using Core.Cache.Pipeline.Behaviors;
using Core.Cache.Redis.Builders;
using Core.Cache.Redis.Diagnostics;
using Core.Cache.Redis.Options;
using Core.Cache.Redis.Resilience;
using Core.Cache.Redis.Storage;
using Core.Cache.Redis.Storage.Abstractions;
using Core.Cache.Storage.Abstractions;
using Core.Observability.Abstractions;
using Core.Redis.Connection;
using Core.Redis.DependencyInjection;
using Core.Redis.Synchronization;
using Core.Resilience.Abstractions;
using Core.Serialization.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Core.Cache.Redis.DependencyInjection;

/// <summary>
/// Registers Redis as the external primary cache storage.
/// </summary>
public static class CacheRedisRegistration
{
    /// <summary>
    /// Registers the Redis connection, storage, tag index, health state and
    /// optional resilience integration.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">
    /// Callback that configures the Redis options. It must assign
    /// <see cref="RedisOptions.Configuration"/>.
    /// </param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <c>AddCoreCache()</c> has not been called, or the callback did not assign
    /// <see cref="RedisOptions.Configuration"/>.
    /// </exception>
    /// <remarks>
    /// When the core cache is registered but disabled, nothing is registered and
    /// no exception is thrown.
    /// </remarks>
    public static IServiceCollection AddCoreCacheRedis(
        this IServiceCollection services,
        Action<RedisOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var cacheOptions = GetCacheOptions(services);

        if (!cacheOptions.Enabled)
        {
            return services;
        }

        var options = BuildOptions(configure);

        services.AddCoreRedis();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var factory =
                sp.GetRequiredService<IRedisConnectionFactory>();

            return factory.Create(options.Configuration!);
        });

        services.AddSingleton<IKeyBuilder>(sp =>
        {

            var prefix = string.IsNullOrWhiteSpace(cacheOptions.InstanceName)
                ? string.Empty
                : $"{cacheOptions.InstanceName}:";

            return new RedisKeyBuilder(prefix);
        });

        services.AddSingleton<RedisTagIndex>();

        services.AddSingleton<ICacheTagIndex<RedisCacheStorage>>(sp =>
            sp.GetRequiredService<RedisTagIndex>());

        services.AddSingleton<IRedisTagIndex>(sp =>
            sp.GetRequiredService<RedisTagIndex>());

        services.AddSingleton<RedisCacheStorage>(sp =>
            new(
                sp.GetRequiredService<IConnectionMultiplexer>(),
                sp.GetRequiredService<IPayloadSerializer>(),
                sp.GetRequiredService<IKeyBuilder>(),
                sp.GetRequiredService<ICacheTagIndex<RedisCacheStorage>>(),
                sp.GetRequiredService<IDistributedLockProvider>(),
                sp.GetRequiredService<ILogger<RedisCacheStorage>>()));

        services.AddSingleton<IExternalCacheStorage>(sp =>
            sp.GetRequiredService<RedisCacheStorage>());

        // Diagnostics
        services.AddSingleton<RedisHealthState>();

        services.AddSingleton<IHealthState>(
            sp => sp.GetRequiredService<RedisHealthState>());

        services.AddSingleton<IPrimaryHealthStateWriter>(
            sp => sp.GetRequiredService<RedisHealthState>());

        services.AddCacheFallback();

        services.AddSingleton<RedisHealthCheck>();
        services.AddSingleton<IHealthCheckContributor>(
            new RedisHealthContributor());

        // Resilience
        var resilienceEnabled =
            services.TryAddRedisCacheResilience();

        if (resilienceEnabled)
        {
            services.AddSingleton<ResilienceBehavior>(sp =>
            {
                var provider =
                    sp.GetRequiredService<IResiliencePipelineProvider>();

                var pipeline =
                    provider.GetPipeline(PipelineType.Redis);

                return new(pipeline);
            });
        }

        return services;
    }

    private static RedisOptions BuildOptions(
    Action<RedisOptions> configure)
    {
        var options = new RedisOptions();

        configure(options);

        if (options.Configuration is null)
        {
            throw new InvalidOperationException(
                RedisMessages.RedisConfigurationRequired);
        }

        return options;
    }

    private static CacheOptions GetCacheOptions(
        IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(
            x => x.ServiceType == typeof(CacheOptions));

        if (descriptor?.ImplementationInstance is not CacheOptions options)
        {
            throw new InvalidOperationException(
                RedisMessages.CacheRegistrationRequired);
        }

        return options;
    }
}
