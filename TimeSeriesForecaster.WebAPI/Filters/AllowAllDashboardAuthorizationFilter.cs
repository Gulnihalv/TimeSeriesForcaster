using Hangfire.Dashboard;

namespace TimeSeriesForecaster.WebAPI.Filters;

// sadece lokalde çalışmak için
public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
