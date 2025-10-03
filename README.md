# .NET Caching Benchmarks

A comprehensive comparison of .NET caching implementations: **IMemoryCache**, **IDistributedCache** (Redis), and **HybridCache**.

## Objective

This application demonstrates and benchmarks three different caching strategies in .NET, allowing you to compare their performance, use cases, and implementation patterns. Each caching approach is exposed through RESTful endpoints to test real-world scenarios with product data.

## How Each Cache Works

### 1. IMemoryCache (In-Memory Cache)

**Implementation:** `MemoryCacheService.cs`

- **Storage:** Data is stored directly in the application's memory
- **Speed:** Fastest option - no serialization or network calls required
- **Scope:** Cache is local to the application instance
- **Persistence:** Data is lost when the application restarts
- **Serialization:** None required - objects are stored as-is in memory
- **Best For:** Single-server applications, frequently accessed data, when low latency is critical

**How it works:**
```csharp
_cache.TryGetValue(key, out T? value);  // Direct memory lookup
_cache.Set(key, value, options);         // Store in memory
```

### 2. IDistributedCache (Redis)

**Implementation:** `DistributedCacheService.cs`

- **Storage:** Data is stored in an external Redis server
- **Speed:** Slower than in-memory due to network I/O and serialization
- **Scope:** Shared across multiple application instances
- **Persistence:** Data survives application restarts (depends on Redis configuration)
- **Serialization:** JSON serialization/deserialization required for complex objects
- **Best For:** Multi-server applications, shared cache scenarios, when persistence is needed

**How it works:**
```csharp
var data = await _cache.GetStringAsync(key);              // Network call to Redis
var obj = JsonSerializer.Deserialize<T>(data);            // Deserialize from JSON
var serialized = JsonSerializer.Serialize(value);         // Serialize to JSON
await _cache.SetStringAsync(key, serialized, options);    // Store in Redis
```

### 3. HybridCache (L1/L2 Two-Tier Cache)

**Implementation:** `HybridCacheService.cs`

- **Storage:** Combines in-memory (L1) and distributed (L2) caching
- **Speed:** Fast for local hits (L1), fallback to distributed (L2) on misses
- **Scope:** Local cache per instance + shared distributed cache
- **Persistence:** L1 is volatile, L2 persists
- **Serialization:** Automatic - handles serialization transparently
- **Best For:** Multi-server applications needing both speed and consistency

**How it works:**
```csharp
// Checks L1 (memory) first, then L2 (distributed), then calls factory
await _cache.GetOrCreateAsync<T>(key, factory, cancellationToken);
// Stores in both L1 and L2 automatically
await _cache.SetAsync(key, value, options, cancellationToken);
```

**Two-tier strategy:**
1. First checks local in-memory cache (L1) - fastest
2. If not found, checks distributed cache (L2) - shared across instances
3. If still not found, executes factory function and populates both tiers
4. Updates to cache are propagated across instances

## Endpoints

- `GET /api/cache/memory/{id}` - Retrieve product using IMemoryCache
- `GET /api/cache/distributed/{id}` - Retrieve product using IDistributedCache (Redis)
- `GET /api/cache/hybrid/{id}` - Retrieve product using HybridCache
- `DELETE /api/cache/{type}/{id}` - Clear cache entry from all cache types

## Prerequisites

- .NET 9.0 or higher
- Redis (for distributed and hybrid caching)
- Docker (optional, for running Redis)

## Running the Application

1. Start Redis using Docker Compose:
   ```bash
   docker-compose up -d
   ```

2. Run the application:
   ```bash
   dotnet run --project src/CachingBenchmarks.Api
   ```

3. Test the endpoints:
   ```bash
   # Memory cache
   curl http://localhost:5000/api/cache/memory/1

   # Distributed cache
   curl http://localhost:5000/api/cache/distributed/1

   # Hybrid cache
   curl http://localhost:5000/api/cache/hybrid/1
   ```

## Configuration

Redis connection string is configured in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```
