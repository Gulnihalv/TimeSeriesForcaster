namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IForecastingService
{
    // Hangfire background job olarak çalışır: eğitilmiş modeli kullanarak
    // 'horizon' kadar ileriye tahmin üretir ve Prediction tablosuna yazar.
    Task ProcessForecastAsync(int modelId, int horizon, CancellationToken cancellationToken = default);
}