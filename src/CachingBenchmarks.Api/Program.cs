using CachingBenchmarks.Api.Models;
using CachingBenchmarks.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

#pragma warning disable EXTEXP0018 // HybridCache is experimental
builder.Services.AddHybridCache(options =>
{
    options.MaximumPayloadBytes = 1024 * 1024; // 1MB
    options.MaximumKeyLength = 512;
});
#pragma warning restore EXTEXP0018

// Register cache services
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddKeyedSingleton<ICacheService, MemoryCacheService>("memory");
builder.Services.AddKeyedSingleton<ICacheService, DistributedCacheService>("distributed");
builder.Services.AddKeyedSingleton<ICacheService, HybridCacheService>("hybrid");

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Sample data generator
Product GetProduct(int id) => new Product(
    id,
    $"Product {id}",
    $"Description for product {id}",
    99.99m * id,
    "Electronics",
    DateTime.UtcNow
);

// Minimal API endpoints
app.MapGet("/api/cache/memory/{id:int}", async (int id, [FromKeyedServices("memory")] ICacheService cache) =>
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
});

app.MapGet("/api/cache/distributed/{id:int}", async (int id, [FromKeyedServices("distributed")] ICacheService cache) =>
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
});

app.MapGet("/api/cache/hybrid/{id:int}", async (int id, [FromKeyedServices("hybrid")] ICacheService cache) =>
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
});

app.MapDelete("/api/cache/{type}/{id:int}", async (string type, int id, [FromKeyedServices("memory")] ICacheService memoryCache,
    [FromKeyedServices("distributed")] ICacheService distributedCache,
    [FromKeyedServices("hybrid")] ICacheService hybridCache) =>
{
    var key = $"product:{id}";

    await Task.WhenAll(
        memoryCache.RemoveAsync(key),
        distributedCache.RemoveAsync(key),
        hybridCache.RemoveAsync(key)
    );

    return Results.NoContent();
});

app.Run();
