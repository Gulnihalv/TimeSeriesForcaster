using Hangfire.Dashboard;

namespace TimeSeriesForecaster.WebAPI.Extensions;

public class DashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
