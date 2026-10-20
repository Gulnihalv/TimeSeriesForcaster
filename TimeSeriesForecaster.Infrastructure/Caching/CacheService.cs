using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TimeSeriesForecaster.Application.Contracts.Application;

namespace TimeSeriesForecaster.Infrastructure.Caching;

public class CacheService : ICacheService
{
    private readonly HybridCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(HybridCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null, CancellationToken ct = default)
    {
        HybridCacheEntryOptions? options = expiration is null
            ? null
            : new HybridCacheEntryOptions
            {
                Expiration = expiration,
                LocalCacheExpiration = expiration < TimeSpan.FromMinutes(5) ? expiration : TimeSpan.FromMinutes(5)
            };

        // factory exception fırlatırsa HybridCache hiçbir şey cache'lemez - hataların cache'lenmemesi için bu davranışa güveniyoruz.
        // Redis (L2) okuma/yazma hatalarını HybridCache kendisi yutup loglar ve factory'ye düşer; burada ek önlem gerekmiyor.
        return await _cache.GetOrCreateAsync(key, async innerCt => await factory(innerCt), options, cancellationToken: ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // GetOrCreateAsync'in aksine HybridCache, Redis erişilemezken RemoveAsync'te exception fırlatıyor.
            // Yutmazsak Redis kapalıyken silme istekleri 500 döner (DB'deki silme zaten kaydedilmiş olsa bile).
            // L1 (bu instance'ın belleği) hata fırlamadan önce temizleniyor; Redis'te kalabilecek bayat kayıt en geç süresi dolunca ölür.
            _logger.LogWarning(ex, "Cache anahtarı silinemedi: {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        // Tekli RemoveAsync hataları yuttuğu için WhenAll fırlatmaz; biri başarısız olsa da diğerleri silinir.
        await Task.WhenAll(keys.Select(key => RemoveAsync(key, ct)));
    }
}
