namespace TimeSeriesForecaster.Application.Common;

// ML servisi çağrısı başarısız olduğunda fırlatılır. Cache factory'lerinde Result yerine exception
// kullanıyoruz: HybridCache, factory hata fırlatırsa hiçbir şey cache'lemez.
public class MlServiceException(string message) : Exception(message);
