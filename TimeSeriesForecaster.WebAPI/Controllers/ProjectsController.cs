using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[Authorize]
public class ProjectsController
{


    [HttpGet]
    public async Task<IActionResult> GetAllProjectsForUser()
    {
        
    }
}
