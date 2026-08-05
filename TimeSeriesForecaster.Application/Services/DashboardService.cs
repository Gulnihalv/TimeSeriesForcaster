using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Application.Services;

public class DashboardService : IDashboardService
{
    private static readonly HashSet<string> ValidEntityTypes = new() { "Dataset", "Model", "Forecast" };

    private readonly IDatasetRepository _datasetRepository;
    private readonly IModelRepository _modelRepository;
    private readonly IDashboardDismissalRepository _dismissalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(
        IDatasetRepository datasetRepository,
        IModelRepository modelRepository,
        IDashboardDismissalRepository dismissalRepository,
        IUnitOfWork unitOfWork)
    {
        _datasetRepository = datasetRepository;
        _modelRepository = modelRepository;
        _dismissalRepository = dismissalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<RecentActivityItemDto>>> GetRecentActivityAsync(int userId, int take = 20)
    {
        var datasets = await _datasetRepository.GetRecentForUserAsync(userId, take);
        var models = await _modelRepository.GetRecentForUserAsync(userId, take);
        var forecasts = await _modelRepository.GetRecentForecastsForUserAsync(userId, take);
        var dismissed = await _dismissalRepository.GetDismissedKeysForUserAsync(userId);

        var items = new List<RecentActivityItemDto>();

        foreach (var dataset in datasets)
        {
            if (dismissed.Contains(("Dataset", dataset.Id))) continue;
            items.Add(MapDataset(dataset));
        }

        foreach (var model in models)
        {
            if (dismissed.Contains(("Model", model.Id))) continue;
            items.Add(MapModel(model));
        }

        foreach (var model in forecasts)
        {
            if (dismissed.Contains(("Forecast", model.Id))) continue;
            items.Add(MapForecast(model));
        }

        var ordered = items
            .OrderByDescending(i => i.CompletedAt ?? i.StartedAt ?? i.CreatedAt)
            .Take(take);

        return Result.Success<IEnumerable<RecentActivityItemDto>>(ordered);
    }

    public async Task<Result> DismissActivityAsync(int userId, string entityType, int entityId)
    {
        if (!ValidEntityTypes.Contains(entityType))
        {
            return Result.Failure(ResultErrorType.BadRequest, ErrorMessages.InvalidEntityType);
        }

        // Forecast, Model tablosundaki aynı satırı (ForecastStatus alanları üzerinden)
        // temsil ettiği için sahiplik kontrolü de Model üzerinden yapılıyor.
        var owns = entityType == "Dataset"
            ? await _datasetRepository.UserOwnsDatasetAsync(entityId, userId)
            : await _modelRepository.UserOwnsModelAsync(entityId, userId);

        if (!owns)
        {
            return Result.Failure(ResultErrorType.Forbidden, ErrorMessages.UnauthorizedAccess);
        }

        var alreadyDismissed = await _dismissalRepository.ExistsAsync(userId, entityType, entityId);
        if (!alreadyDismissed)
        {
            _dismissalRepository.CreateDismissal(new DashboardDismissal
            {
                UserId = userId,
                EntityType = entityType,
                EntityId = entityId,
                DismissedAt = DateTime.UtcNow,
            });
            await _unitOfWork.SaveChangesAsync();
        }

        return Result.Success();
    }

    private static RecentActivityItemDto MapDataset(Dataset dataset)
    {
        var status = dataset.ErrorMessage != null
            ? DashboardActivityStatus.Failed
            : dataset.IsProcessed
                ? DashboardActivityStatus.Completed
                : DashboardActivityStatus.Processing;

        return new RecentActivityItemDto
        {
            EntityType = "Dataset",
            EntityId = dataset.Id,
            ProjectId = dataset.ProjectId,
            DatasetId = null,
            Name = dataset.Name,
            Status = status,
            ProgressPercentage = dataset.ProgressPercentage,
            ErrorMessage = dataset.ErrorMessage,
            CreatedAt = dataset.CreatedAt,
            StartedAt = dataset.CreatedAt,
            CompletedAt = dataset.IsProcessed || dataset.ErrorMessage != null ? dataset.UpdatedAt : null,
        };
    }

    private static RecentActivityItemDto MapModel(Model model)
    {
        var status = model.Status switch
        {
            ModelStatus.Queued => DashboardActivityStatus.Queued,
            ModelStatus.Training => DashboardActivityStatus.Processing,
            ModelStatus.Completed => DashboardActivityStatus.Completed,
            ModelStatus.Failed => DashboardActivityStatus.Failed,
            ModelStatus.Cancelled => DashboardActivityStatus.Cancelled,
            _ => DashboardActivityStatus.Queued,
        };

        return new RecentActivityItemDto
        {
            EntityType = "Model",
            EntityId = model.Id,
            ProjectId = model.ProjectId,
            DatasetId = model.DatasetId,
            Name = model.ModelName,
            Status = status,
            ProgressPercentage = model.ProgressPercentage,
            ErrorMessage = model.ErrorMessage,
            CreatedAt = model.CreatedAt,
            StartedAt = model.TrainingStartedAt,
            CompletedAt = model.TrainingCompletedAt,
        };
    }

    private static RecentActivityItemDto MapForecast(Model model)
    {
        var status = model.ForecastStatus switch
        {
            ForecastStatus.Queued => DashboardActivityStatus.Queued,
            ForecastStatus.Generating => DashboardActivityStatus.Processing,
            ForecastStatus.Completed => DashboardActivityStatus.Completed,
            ForecastStatus.Failed => DashboardActivityStatus.Failed,
            ForecastStatus.Cancelled => DashboardActivityStatus.Cancelled,
            _ => DashboardActivityStatus.Queued,
        };

        return new RecentActivityItemDto
        {
            EntityType = "Forecast",
            EntityId = model.Id,
            ProjectId = model.ProjectId,
            DatasetId = model.DatasetId,
            Name = model.ModelName,
            Status = status,
            ProgressPercentage = model.ForecastProgressPercentage,
            ErrorMessage = model.ForecastErrorMessage,
            CreatedAt = model.CreatedAt,
            StartedAt = model.ForecastStartedAt,
            CompletedAt = model.ForecastCompletedAt,
        };
    }
}
