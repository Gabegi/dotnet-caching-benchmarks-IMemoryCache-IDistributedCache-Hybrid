using CachingBenchmarks.Api.Models;
using CachingBenchmarks.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CachingBenchmarks.Api.Endpoints;

public static class CacheEndpoints
{
    public static RouteGroupBuilder MapCacheEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/cache");

        group.MapGet("/memory/{id:int}", GetFromMemoryCacheAsync);
        group.MapGet("/distributed/{id:int}", GetFromDistributedCacheAsync);
        group.MapGet("/hybrid/{id:int}", GetFromHybridCacheAsync);
        group.MapDelete("/{type}/{id:int}", DeleteFromAllCachesAsync);

        return group;
    }

    private static async Task<IResult> GetFromMemoryCacheAsync(
        int id,
        [FromKeyedServices("memory")] ICacheService cache)
    {
        var key = $"product:{id}";
        var product = await cache.GetAsync<Product>(key);

        if (product is null)
        {
            product = GetProduct(id);
            await cache.SetAsync(key, product, TimeSpan.FromMinutes(5));
            return Results.Ok(new { source = "database", product });
        }

        return Results.Ok(new { source = "cache", product });
    }

    private static async Task<IResult> GetFromDistributedCacheAsync(
        int id,
        [FromKeyedServices("distributed")] ICacheService cache)
    {
        var key = $"product:{id}";
        var product = await cache.GetAsync<Product>(key);

        if (product is null)
        {
            product = GetProduct(id);
            await cache.SetAsync(key, product, TimeSpan.FromMinutes(5));
            return Results.Ok(new { source = "database", product });
        }

        return Results.Ok(new { source = "cache", product });
    }

    private static async Task<IResult> GetFromHybridCacheAsync(
        int id,
        [FromKeyedServices("hybrid")] ICacheService cache)
    {
        var key = $"product:{id}";
        var product = await cache.GetAsync<Product>(key);

        if (product is null)
        {
            product = GetProduct(id);
            await cache.SetAsync(key, product, TimeSpan.FromMinutes(5));
            return Results.Ok(new { source = "database", product });
        }

        return Results.Ok(new { source = "cache", product });
    }

    private static async Task<IResult> DeleteFromAllCachesAsync(
        string type,
        int id,
        [FromKeyedServices("memory")] ICacheService memoryCache,
        [FromKeyedServices("distributed")] ICacheService distributedCache,
        [FromKeyedServices("hybrid")] ICacheService hybridCache)
    {
        var key = $"product:{id}";

        await Task.WhenAll(
            memoryCache.RemoveAsync(key),
            distributedCache.RemoveAsync(key),
            hybridCache.RemoveAsync(key)
        );

        return Results.NoContent();
    }

    private static Product GetProduct(int id) => new Product(
        id,
        $"Product {id}",
        $"Description for product {id}",
        99.99m * id,
        "Electronics",
        DateTime.UtcNow
    );
}
