using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.WebAPI.Extensions;

namespace TimeSeriesForecaster.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
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
            return Unauthorized("User id bulunmadı"); // hata kodları için dosya oluşturmak lazım.
        }

        var results = await _projectService.GetProjectsForUserAsync(userId.Value);

        return Ok(results);
    }

    [HttpGet("{id}", Name = "GetProjectById")]
    public async Task<IActionResult> GetProjectById(int id)
    {
        var userId = User.GetUserId();

        if (userId == null)
        {
            return Unauthorized("User id bulunmadı");
        }

        var project = await _projectService.GetProjectByIdAsync(id, userId.Value);
        if (project == null)
        {
            return NotFound("Proje bulunamadı veya bu kullanıcıya ait değil.");
        }

        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProjectForUser([FromBody] ProjectForCreationDto projectForCreationDto)
    {
        var userId = User.GetUserId();

        if (userId == null)
        {
            return Unauthorized("User id bulunmadı"); // hata kodları için dosya oluşturmak lazım.
        }

        var result = await _projectService.CreateProjectForUserAsync(projectForCreationDto, userId.Value);

        if (result == null)
        {
            return BadRequest("Proje oluşturulamadı.");
        }

        return CreatedAtAction(nameof(GetProjectById), new { id = result.Id }, result); 
    }
}
