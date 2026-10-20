namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default);
    // Cache altyapısı (Redis) erişilemezse fırlatmaz, loglar: cache temizliği başarısız diye iş akışı bozulmamalı.
    Task RemoveAsync(string key, CancellationToken ct = default);

    // Anahtarları paralel siler; Redis erişilemezken toplam süre anahtar sayısıyla değil, tek bir zaman aşımıyla sınırlı kalır.
    Task RemoveAsync(IEnumerable<string> keys, CancellationToken ct = default);
}
