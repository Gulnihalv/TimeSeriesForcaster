using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Contracts.Persistence;

public interface IProjectRepository
{
    Task<Project?> GetProjectByIdAsync(int id, bool trackChanges);
    Task<IEnumerable<Project>> GetAllProjectsForUserAsync(int userId, bool trackChanges, bool includeInactive = false); // includeInactive eklendi soft delete için
    void CreateProject(Project project);
    void UpdateProject(Project project);
    void RemoveProject(Project project);
    Task<bool> ProjectExistsAsync(int id);
    Task<bool> UserOwnsProjectAsync(int projectId, int userId);
}
