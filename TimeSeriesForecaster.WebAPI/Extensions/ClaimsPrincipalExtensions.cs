using System.Security.Claims;

namespace TimeSeriesForecaster.WebAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        if (user == null)
        {
            return null;
        }

        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return null;
    }
}
