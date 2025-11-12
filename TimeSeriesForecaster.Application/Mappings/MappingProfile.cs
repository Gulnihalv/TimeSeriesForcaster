using AutoMapper;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Project, ProjectDto>();
        CreateMap<ProjectForCreationDto, Project>();
        CreateMap<Dataset, DatasetDto>();
        CreateMap<Model, ModelDto>();
    }
}
