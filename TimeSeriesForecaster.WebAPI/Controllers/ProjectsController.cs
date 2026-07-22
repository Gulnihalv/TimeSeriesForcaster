using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.WebAPI.Extensions;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/projects")]
public class ProjectsController : ApiControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllProjectsForUser()
    {
        var userId = User.GetUserId();

        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _projectService.GetProjectsForUserAsync(userId.Value);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpGet("{id}", Name = "GetProjectById")]
    public async Task<IActionResult> GetProjectById(int id)
    {
        var userId = User.GetUserId();

        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _projectService.GetProjectByIdAsync(id, userId.Value);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProjectForUser([FromBody] ProjectForCreationDto projectForCreationDto)
    {
        var userId = User.GetUserId();

        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _projectService.CreateProjectForUserAsync(projectForCreationDto, userId.Value);
        return ToActionResult(result, value => CreatedAtAction(nameof(GetProjectById), new { id = value!.Id }, value));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var userId = User.GetUserId();

        if (userId == null)
        {
            return Unauthorized(ErrorMessages.UnauthorizedAccess);
        }

        var result = await _projectService.DeleteProjectAsync(id, userId.Value);
        return ToActionResult(result);
    }
}