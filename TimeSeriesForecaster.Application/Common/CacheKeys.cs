namespace TimeSeriesForecaster.Application.Common;

// Anahtarlar tek bir yerde: yazarken ve silerken aynı üretici kullanılsın diye.
// İkisi farklı yerlerde elle yazılırsa, bir harf farkı yüzünden cache sessizce hiç temizlenmez.
public static class CacheKeys
{
    private const string Prefix = "tsf";

    // DatasetsController'daki maxPoints varsayılanı da bu sabiti kullanır; ikisi ayrı yaşasaydı biri değişince
    // cache sessizce hiç çalışmazdı (her istek listede olmayan bir değerle gelirdi).
    public const int DefaultDataPointLimit = 2000;

    // Datapoints anahtarı maxPoints içerdiği için her değer ayrı bir anahtar demek. maxPoints istemciden geldiği için
    // keyfi değerler cache'lenirse silerken hangi varyantların var olduğunu bilemeyiz. Bu yüzden sadece burada listelenen
    // değerler cache'lenir (frontend maxPoints göndermiyor, varsayılan kullanılıyor); diğerleri doğrudan DB'ye gider.
    public static readonly IReadOnlyList<int> CacheableDataPointLimits = [DefaultDataPointLimit];

    public static string ModelComponents(int modelId) => $"{Prefix}:model:{modelId}:components";

    public static string DatasetStatistics(int datasetId) => $"{Prefix}:dataset:{datasetId}:statistics";
    public static string DatasetResolutionCounts(int datasetId) => $"{Prefix}:dataset:{datasetId}:resolution-counts";
    public static string DatasetDataPoints(int datasetId, int? maxPoints) => $"{Prefix}:dataset:{datasetId}:datapoints:{maxPoints?.ToString() ?? "all"}";

    // Bir dataset'in verisi değiştiğinde (yeniden işleme, silme) temizlenmesi gereken tüm anahtarlar.
    public static IEnumerable<string> AllForDataset(int datasetId)
    {
        yield return DatasetStatistics(datasetId);
        yield return DatasetResolutionCounts(datasetId);
        foreach (var limit in CacheableDataPointLimits)
        {
            yield return DatasetDataPoints(datasetId, limit);
        }
    }
}
