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
        CreateMap<DataPoint, DataPointDto>();
        CreateMap<Model, ModelDto>();
        CreateMap<Prediction, PredictionDto>();
        CreateMap<ModelMetric, ModelMetricDto>();
        CreateMap<Model, ModelDetailDto>()
            .ForMember(dest => dest.Metrics, opt => opt.MapFrom(src => src.ModelMetrics));
    }
}