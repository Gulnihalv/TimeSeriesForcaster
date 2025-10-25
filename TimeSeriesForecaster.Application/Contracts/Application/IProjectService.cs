using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetProjectsForUserAsync(int userId);
    Task<ProjectDto> CreateProjectForUserAsync(ProjectForCreationDto projectDto, int userId);
    Task<ProjectDto?> GetProjectByIdAsync(int projectId, int userId);
}
