using Microsoft.Extensions.Logging;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.Contracts.Application;

namespace TimeSeriesForecaster.Application.Services;

// Gerçek e-posta göndermiyor, sadece loglar. SMTP/SendGrid entegrasyonu eklenince
// bu sınıfın yerine gerçek implementasyon DI'da tek satır değiştirilerek takılır.
public class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger)
    {
        _logger = logger;
    }

    public Task<Result> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOpEmailService] E-posta gönderilecekti: To={ToEmail}, Subject={Subject}", toEmail, subject);
        return Task.FromResult(Result.Success());
    }
}