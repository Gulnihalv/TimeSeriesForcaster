using TimeSeriesForecaster.Application.Common;

namespace TimeSeriesForecaster.Application.Contracts.Application;

// İleride gerçek bir SMTP/SendGrid implementasyonu bu interface'i implement edecek.
public interface IEmailService
{
    Task<Result> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}