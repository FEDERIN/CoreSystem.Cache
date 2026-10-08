using Core.Cache.Storage.Abstractions;
using Core.Cache.Storage.Memory;
using Core.Memory.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Cache.DependencyInjection;

internal static class MemoryRegistration
{
    public static IServiceCollection AddCacheMemory(
        this IServiceCollection services)
    {
        services.AddMemoryCache();

        // Entry
        services.AddSingleton<ICacheEntryFactory, CacheEntryFactory>();
        services.AddSingleton<ICacheEntryInspector, CacheEntryInspector>();

        // Tags
        services.AddSingleton<ICacheTagIndex<MemoryStorage>, MemoryTagIndex>();

        // Key tracking. This is the seam CoreSystem.Cache.Rehydration reads to
        // enumerate the entries the Memory storage holds, so it is required by
        // MemoryRehydrationSource in that package. Rehydration itself registers
        // its own source, target and service and belongs to that package.
        services.AddSingleton<ICacheKeyTracker, MemoryKeyTracker>();

        services.AddCoreMemory();

        // Storage
        services.AddSingleton<MemoryStorage>();

        return services;
    }
}