using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.WebAPI.Extensions;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _dashboardService.GetRecentActivityAsync(userId.Value);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpPost("recent-activity/dismiss")]
    public async Task<IActionResult> DismissActivity([FromBody] DismissActivityRequestDto request)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _dashboardService.DismissActivityAsync(userId.Value, request.EntityType, request.EntityId);
        return ToActionResult(result);
    }
}
