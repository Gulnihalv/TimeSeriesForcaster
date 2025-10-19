using Microsoft.EntityFrameworkCore;
using TimeSeriesForecaster.Application.Interfaces;
using TimeSeriesForecaster.Domain.Entities;
using TimeSeriesForecaster.Infrastructure.Persistence;

namespace TimeSeriesForecaster.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public void CreateProject(Project project) => _context.Projects.Add(project);
    public void RemoveProject(Project project) => _context.Projects.Remove(project);
    public void UpdateProject(Project project) => _context.Projects.Update(project);

    public async Task<Project?> GetProjectByIdAsync(int id, bool trackChanges)
    {
        IQueryable<Project> query = _context.Projects
            .Include(p => p.Datasets);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Project>> GetAllProjectsForUserAsync(int userId, bool trackChanges, bool includeInactive = false)
    {
        IQueryable<Project> query = trackChanges 
            ? _context.Projects 
            : _context.Projects.AsNoTracking();

        query = query.Where(p => p.UserId == userId);

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }
        
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<bool> ProjectExistsAsync(int id)
    {
        return await _context.Projects.AnyAsync(p => p.Id == id);
    }

    public async Task<bool> UserOwnsProjectAsync(int projectId, int userId)
    {
        return await _context.Projects.AnyAsync(p => p.Id == projectId && p.UserId == userId);
    }
}
