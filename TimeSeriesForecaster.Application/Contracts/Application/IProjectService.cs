using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.DTOs;

namespace TimeSeriesForecaster.Application.Contracts.Application;

public interface IProjectService
{
    Task<Result<IEnumerable<ProjectDto>>> GetProjectsForUserAsync(int userId);
    Task<Result<ProjectDto>> CreateProjectForUserAsync(ProjectForCreationDto projectDto, int userId);
    Task<Result<ProjectDto?>> GetProjectByIdAsync(int projectId, int userId);
    Task<Result<bool>> DeleteProjectAsync(int projectId, int userId);
}