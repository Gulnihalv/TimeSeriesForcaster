using AutoMapper;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectRepository _projectRepository;
    private readonly IMapper _mapper;

    public ProjectService(IUnitOfWork unitOfWork, IProjectRepository projectRepository, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _projectRepository = projectRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProjectDto>> GetProjectsForUserAsync(int userId)
    {
        var projects = await _projectRepository.GetAllProjectsForUserAsync(userId: userId, trackChanges: false, includeInactive: true); // Burası readonly bir method olduğunan trackchangesi false yaptım.

        var projectsDto = _mapper.Map<IEnumerable<ProjectDto>>(projects);
        return projectsDto;
    }
    public async Task<ProjectDto> CreateProjectForUserAsync(ProjectForCreationDto projectDto, int userId)
    {
        var projectEntity = _mapper.Map<Project>(projectDto);

        projectEntity.UserId = userId;
        projectEntity.CreatedAt = DateTime.UtcNow;
        projectEntity.UpdatedAt = DateTime.UtcNow;
        projectEntity.IsActive = true;

        _projectRepository.CreateProject(projectEntity);
        await _unitOfWork.SaveChangesAsync();

        var projectResultDto = _mapper.Map<ProjectDto>(projectEntity);
        return projectResultDto;
    } 
}
