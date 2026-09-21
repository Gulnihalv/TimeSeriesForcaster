using AutoMapper;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using NSubstitute;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.Configuration;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Application.DTOs;
using TimeSeriesForecaster.Application.Services;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Tests.Services;

public class ModelServiceTests
{
    private const int DatasetId = 7;
    private const int UserId = 42;
    private const int ModelId = 11;

    private readonly IModelRepository _modelRepository = Substitute.For<IModelRepository>();
    private readonly IDatasetRepository _datasetRepository = Substitute.For<IDatasetRepository>();
    private readonly IDataPointRepository _dataPointRepository = Substitute.For<IDataPointRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IBackgroundJobClient _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();

    // Varsayılan dataset ham haliyle eğitilebilir boyutta. Çözünürlük gönderilmeyen testler
    // (ör. hiperparametre testleri) artık ham nokta sayısı (RecordCount) üzerinden doğrulamadan geçiyor.
    private readonly Dataset _dataset = new() { Id = DatasetId, ProjectId = 3, IsProcessed = true, RecordCount = 1_000 };

    private readonly ModelService _sut;

    public ModelServiceTests()
    {
        _sut = new ModelService(_modelRepository, _datasetRepository, _dataPointRepository, _unitOfWork, _mapper, _backgroundJobClient, _httpClientFactory);

        // Enqueue<T>() extension'ı sonunda IBackgroundJobClient.Create(Job, IState) çağırır ve job id bekler.
        _backgroundJobClient.Create(Arg.Any<Job>(), Arg.Any<IState>()).Returns("job-1");

        // Ayarlanmazsa Map<ModelDto> null döner; servis "başarılı" sonucu içinde null DTO taşımasın diye varsayılan bir dönüş veriyoruz.
        _mapper.Map<ModelDto>(Arg.Any<object>()).Returns(new ModelDto());

        _datasetRepository.GetDatasetByIdAsync(DatasetId, Arg.Any<bool>()).Returns(_dataset);
    }

    // ---------- TrainModelAsync: yetkilendirme ----------

    [Fact]
    public async Task TrainModelAsync_UserDoesNotOwnDataset_ReturnsForbidden()
    {
        _datasetRepository.UserOwnsDatasetAsync(DatasetId, UserId).Returns(false);

        var result = await _sut.TrainModelAsync(DatasetId, UserId, "Prophet");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorType.Forbidden, result.ErrorType);
        Assert.Equal(ErrorMessages.UnauthorizedAccess, result.Error);
    }

    [Fact]
    public async Task TrainModelAsync_UserDoesNotOwnDataset_DoesNotCreateModelOrEnqueueJob()
    {
        _datasetRepository.UserOwnsDatasetAsync(DatasetId, UserId).Returns(false);

        await _sut.TrainModelAsync(DatasetId, UserId, "Prophet");

        AssertNothingCreatedOrEnqueued();
    }

    [Fact]
    public async Task TrainModelAsync_DatasetNotFound_ReturnsNotFound()
    {
        SetupOwnedDataset();
        _datasetRepository.GetDatasetByIdAsync(DatasetId, Arg.Any<bool>()).Returns((Dataset?)null);

        var result = await _sut.TrainModelAsync(DatasetId, UserId, "Prophet");

        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
        AssertNothingCreatedOrEnqueued();
    }

    // ---------- TrainModelAsync: hiperparametreler ----------

    [Fact]
    public async Task TrainModelAsync_WithHyperparameters_SerializesAsCamelCaseJson()
    {
        // Regresyon kilidi: DB'de saklanan bu JSON'u frontend'deki model karşılaştırma ekranı camelCase
        // anahtarlarla okuyor. (Python'a gönderilirken ModelProcessingService snake_case'e çeviriyor; o ayrı bir sözleşme.)
        SetupOwnedDataset();
        var hyperparameters = new ProphetHyperparametersDto
        {
            SeasonalityMode = "multiplicative",
            ChangepointPriorScale = 0.05,
            SeasonalityPriorScale = 0.1,
            ChangepointRange = 0.8
        };

        await _sut.TrainModelAsync(DatasetId, UserId, "Prophet", hyperparameters);

        var model = GetCreatedModel();
        Assert.Equal(
            """{"seasonalityMode":"multiplicative","changepointPriorScale":0.05,"seasonalityPriorScale":0.1,"changepointRange":0.8}""",
            model.Hyperparameters);
    }

    [Fact]
    public async Task TrainModelAsync_WithHyperparameters_DoesNotUsePascalCaseKeys()
    {
        SetupOwnedDataset();
        var hyperparameters = new ProphetHyperparametersDto { SeasonalityMode = "additive" };

        await _sut.TrainModelAsync(DatasetId, UserId, "Prophet", hyperparameters);

        var json = GetCreatedModel().Hyperparameters!;
        Assert.Contains("\"seasonalityMode\"", json);
        Assert.DoesNotContain("\"SeasonalityMode\"", json);
        Assert.DoesNotContain("seasonality_mode", json);
    }

    [Fact]
    public async Task TrainModelAsync_WithoutHyperparameters_LeavesHyperparametersNull()
    {
        SetupOwnedDataset();

        await _sut.TrainModelAsync(DatasetId, UserId, "Prophet");

        Assert.Null(GetCreatedModel().Hyperparameters);
    }

    // ---------- TrainModelAsync: çözünürlük validasyonu ----------

    [Fact]
    public async Task TrainModelAsync_DisallowedResolution_ReturnsBadRequest()
    {
        // Minute çözünürlüğü izin verilen üst sınırı aşıyor, Day ise uygun.
        SetupOwnedDataset(pointCounts: PointCounts(minute: ResolutionSettings.MaxPointsForAllowed + 1, day: 1_000));

        var result = await _sut.TrainModelAsync(DatasetId, UserId, "Prophet", timeResolution: TimeResolution.Minute);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
    }

    [Fact]
    public async Task TrainModelAsync_DisallowedResolution_DoesNotCreateModelOrEnqueueJob()
    {
        SetupOwnedDataset(pointCounts: PointCounts(minute: ResolutionSettings.MaxPointsForAllowed + 1, day: 1_000));

        await _sut.TrainModelAsync(DatasetId, UserId, "Prophet", timeResolution: TimeResolution.Minute);

        AssertNothingCreatedOrEnqueued();
    }

    [Fact]
    public async Task TrainModelAsync_ResolutionBelowMinimumPoints_ReturnsBadRequest()
    {
        SetupOwnedDataset(pointCounts: PointCounts(week: ResolutionSettings.MinPointsForAllowed - 1, day: 1_000));

        var result = await _sut.TrainModelAsync(DatasetId, UserId, "Prophet", timeResolution: TimeResolution.Week);

        Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
        _modelRepository.DidNotReceiveWithAnyArgs().CreateModel(default!);
    }

    [Fact]
    public async Task TrainModelAsync_NoResolutionAndRawCountAboveLimit_ReturnsBadRequestAndCreatesNothing()
    {
        // Açık kapatıldı: çözünürlük gönderilmemesi validasyonu atlatmamalı. Aksi halde API'yi doğrudan
        // çağıran biri milyonlarca satırlık bir dataset'i ham haliyle eğitime sokabilirdi.
        SetupOwnedDataset();
        _dataset.RecordCount = ResolutionSettings.MaxPointsForAllowed + 1;

        var result = await _sut.TrainModelAsync(DatasetId, UserId, "Prophet");

        Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
        AssertNothingCreatedOrEnqueued();
    }

    [Fact]
    public async Task TrainModelAsync_NoResolution_UsesRecordCountWithoutQueryingResolutionCounts()
    {
        // Ham nokta sayısı dataset'te zaten saklı; pahalı COUNT(DISTINCT ...) sorgusu çalıştırılmamalı.
        SetupOwnedDataset();

        var result = await _sut.TrainModelAsync(DatasetId, UserId, "Prophet");

        Assert.True(result.IsSuccess);
        Assert.Null(GetCreatedModel().TrainingResolution);
        Assert.Single(GetEnqueuedJobs());
        await _dataPointRepository.DidNotReceiveWithAnyArgs().GetResolutionPointCountsAsync(default);
    }

    [Fact]
    public async Task TrainModelAsync_AllowedResolution_CreatesModelWithResolutionAndEnqueuesProcessing()
    {
        SetupOwnedDataset(pointCounts: PointCounts(day: 1_000));

        var result = await _sut.TrainModelAsync(DatasetId, UserId, "Prophet", timeResolution: TimeResolution.Day, aggregationFunction: AggregationFunction.Sum);

        Assert.True(result.IsSuccess);
        var model = GetCreatedModel();
        Assert.Equal(TimeResolution.Day, model.TrainingResolution);
        Assert.Equal(AggregationFunction.Sum, model.TrainingAggregation);
        Assert.Equal(ModelStatus.Queued, model.Status);
        Assert.Equal("job-1", model.HangfireJobId);

        var job = Assert.Single(GetEnqueuedJobs());
        Assert.Equal(typeof(IModelProcessingService), job.Type);
        Assert.Equal(nameof(IModelProcessingService.ProcessModelAsync), job.Method.Name);
    }

    [Fact]
    public async Task TrainModelAsync_AggregatedResolutionWithoutAggregation_DefaultsToAverage()
    {
        // Açık kapatıldı: agregasyon gönderilmezse job None ile kuyruğa giriyor ve agregasyon sorgusunda patlıyordu.
        SetupOwnedDataset(pointCounts: PointCounts(day: 1_000));

        await _sut.TrainModelAsync(DatasetId, UserId, "Prophet", timeResolution: TimeResolution.Day);

        Assert.Equal(AggregationFunction.Average, GetCreatedModel().TrainingAggregation);
        var job = Assert.Single(GetEnqueuedJobs());
        Assert.Contains(AggregationFunction.Average, job.Args);
    }

    [Fact]
    public async Task TrainModelAsync_RawWithAggregation_IgnoresAggregation()
    {
        // Ham veride agregasyon anlamsız; gönderilse bile saklanmamalı ve job'a None gitmeli.
        SetupOwnedDataset();

        await _sut.TrainModelAsync(DatasetId, UserId, "Prophet", timeResolution: TimeResolution.Raw, aggregationFunction: AggregationFunction.Sum);

        Assert.Null(GetCreatedModel().TrainingAggregation);
        var job = Assert.Single(GetEnqueuedJobs());
        Assert.Contains(AggregationFunction.None, job.Args);
    }

    // ---------- GetResolutionOptionsAsync: eşik mantığı ----------

    [Fact]
    public async Task GetResolutionOptionsAsync_AllCountsWithinBounds_AllAllowedAndOneRecommended()
    {
        SetupOwnedDataset(pointCounts: PointCounts(raw: 50_000, minute: 20_000, hour: 5_000, day: 1_000, week: 200, month: 50));

        var result = await _sut.GetResolutionOptionsAsync(DatasetId, UserId);

        Assert.True(result.IsSuccess);
        var options = result.Value!;
        Assert.Equal(Enum.GetValues<TimeResolution>().Length, options.Count);
        Assert.All(options, o => Assert.True(o.IsAllowed));
        Assert.Single(options, o => o.IsRecommended);
    }

    [Fact]
    public async Task GetResolutionOptionsAsync_NoCountWithinBounds_NoneAllowedAndNoneRecommended()
    {
        SetupOwnedDataset(pointCounts: PointCounts(
            raw: 0,
            minute: ResolutionSettings.MinPointsForAllowed - 1,
            hour: ResolutionSettings.MaxPointsForAllowed + 1,
            day: 5,
            week: 1_000_000,
            month: 0));

        var result = await _sut.GetResolutionOptionsAsync(DatasetId, UserId);

        var options = result.Value!;
        Assert.All(options, o => Assert.False(o.IsAllowed));
        Assert.DoesNotContain(options, o => o.IsRecommended);
    }

    [Fact]
    public async Task GetResolutionOptionsAsync_SeveralCountsInRecommendedRange_ExactlyOneRecommended()
    {
        // Raw, Minute, Hour ve Day tavsiye aralığında (eşitlik durumu); yine de tek bir öneri olmalı.
        SetupOwnedDataset(pointCounts: PointCounts(raw: 1_000, minute: 2_000, hour: 3_000, day: 4_000, week: 100, month: 50));

        var result = await _sut.GetResolutionOptionsAsync(DatasetId, UserId);

        Assert.Single(result.Value!, o => o.IsRecommended);
    }

    [Fact]
    public async Task GetResolutionOptionsAsync_NoCountInRecommendedRange_RecommendsClosestAllowedOption()
    {
        // Raw/Minute/Hour tavsiye aralığının üstünde, Day (300) alt sınıra en yakın (500'e 200 uzak).
        // Week (20) ve Month (10) izinli ama daha uzak.
        SetupOwnedDataset(pointCounts: PointCounts(raw: 90_000, minute: 30_000, hour: 25_000, day: 300, week: 20, month: 10));

        var result = await _sut.GetResolutionOptionsAsync(DatasetId, UserId);

        var recommended = Assert.Single(result.Value!, o => o.IsRecommended);
        Assert.Equal(TimeResolution.Day, recommended.Resolution);
    }

    [Fact]
    public async Task GetResolutionOptionsAsync_DisallowedOptionIsNeverRecommended()
    {
        // Raw (500_000) ve Minute (200_000) izinsiz; öneri yalnızca izinli seçenekler (Hour, Day) arasından yapılmalı.
        SetupOwnedDataset(pointCounts: PointCounts(raw: 500_000, minute: 200_000, hour: 30_000, day: 25_000, week: 5, month: 5));

        var result = await _sut.GetResolutionOptionsAsync(DatasetId, UserId);

        var options = result.Value!;
        Assert.DoesNotContain(options, o => o.IsRecommended && !o.IsAllowed);
        var recommended = Assert.Single(options, o => o.IsRecommended);
        Assert.Equal(TimeResolution.Day, recommended.Resolution); // 25_000 → 5_000 uzak, Hour 30_000 → 10_000 uzak
    }

    [Fact]
    public async Task GetResolutionOptionsAsync_CountsOnAllowedBoundaries_AreInclusive()
    {
        SetupOwnedDataset(pointCounts: PointCounts(
            raw: ResolutionSettings.MinPointsForAllowed,          // tam alt sınır → izinli
            minute: ResolutionSettings.MaxPointsForAllowed,       // tam üst sınır → izinli
            hour: ResolutionSettings.MinPointsForAllowed - 1,     // alt sınırın 1 altı → izinsiz
            day: ResolutionSettings.MaxPointsForAllowed + 1,      // üst sınırın 1 üstü → izinsiz
            week: 50,
            month: 50));

        var result = await _sut.GetResolutionOptionsAsync(DatasetId, UserId);

        var byResolution = result.Value!.ToDictionary(o => o.Resolution);
        Assert.True(byResolution[TimeResolution.Raw].IsAllowed);
        Assert.True(byResolution[TimeResolution.Minute].IsAllowed);
        Assert.False(byResolution[TimeResolution.Hour].IsAllowed);
        Assert.False(byResolution[TimeResolution.Day].IsAllowed);
    }

    [Fact]
    public async Task GetResolutionOptionsAsync_MissingResolutionInCounts_TreatedAsZeroAndDisallowed()
    {
        // Sözlükte olmayan bir çözünürlük sessizce başka bir çözünürlüğün sayısını almamalı (pozisyonel eşlemenin riski buydu).
        SetupOwnedDataset(pointCounts: new Dictionary<TimeResolution, int> { [TimeResolution.Day] = 1_000 });

        var result = await _sut.GetResolutionOptionsAsync(DatasetId, UserId);

        var byResolution = result.Value!.ToDictionary(o => o.Resolution);
        Assert.Equal(0, byResolution[TimeResolution.Hour].EstimatedPoints);
        Assert.False(byResolution[TimeResolution.Hour].IsAllowed);
        Assert.True(byResolution[TimeResolution.Day].IsAllowed);
    }

    [Fact]
    public async Task GetResolutionOptionsAsync_UserDoesNotOwnDataset_ReturnsForbidden()
    {
        _datasetRepository.UserOwnsDatasetAsync(DatasetId, UserId).Returns(false);

        var result = await _sut.GetResolutionOptionsAsync(DatasetId, UserId);

        Assert.Equal(ResultErrorType.Forbidden, result.ErrorType);
        await _dataPointRepository.DidNotReceiveWithAnyArgs().GetResolutionPointCountsAsync(default);
    }

    // ---------- GenerateForecastAsync: durum kontrolü ----------

    [Theory]
    [InlineData(ModelStatus.Queued)]
    [InlineData(ModelStatus.Training)]
    [InlineData(ModelStatus.Failed)]
    [InlineData(ModelStatus.Cancelled)]
    public async Task GenerateForecastAsync_ModelNotCompleted_ReturnsValidationErrorAndDoesNotEnqueue(ModelStatus status)
    {
        SetupOwnedModel(new Model { Id = ModelId, Status = status });

        var result = await _sut.GenerateForecastAsync(ModelId, UserId, horizon: 30);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorType.ValidationError, result.ErrorType);
        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task GenerateForecastAsync_ModelNotFound_ReturnsValidationErrorAndDoesNotEnqueue()
    {
        SetupOwnedModel(null);

        var result = await _sut.GenerateForecastAsync(ModelId, UserId, horizon: 30);

        Assert.Equal(ResultErrorType.ValidationError, result.ErrorType);
        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task GenerateForecastAsync_UserDoesNotOwnModel_ReturnsForbidden()
    {
        _modelRepository.UserOwnsModelAsync(ModelId, UserId).Returns(false);

        var result = await _sut.GenerateForecastAsync(ModelId, UserId, horizon: 30);

        Assert.Equal(ResultErrorType.Forbidden, result.ErrorType);
        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task GenerateForecastAsync_ModelCompleted_QueuesForecastAndEnqueuesForecastingService()
    {
        var model = new Model { Id = ModelId, Status = ModelStatus.Completed };
        SetupOwnedModel(model);

        var result = await _sut.GenerateForecastAsync(ModelId, UserId, horizon: 30);

        Assert.True(result.IsSuccess);
        Assert.Equal(ForecastStatus.Queued, model.ForecastStatus);
        Assert.Equal(0, model.ForecastProgressPercentage);

        var job = Assert.Single(GetEnqueuedJobs());
        Assert.Equal(typeof(IForecastingService), job.Type);
        Assert.Equal(nameof(IForecastingService.ProcessForecastAsync), job.Method.Name);
    }

    // ---------- Yardımcılar ----------

    private void SetupOwnedDataset(IReadOnlyDictionary<TimeResolution, int>? pointCounts = null)
    {
        _datasetRepository.UserOwnsDatasetAsync(DatasetId, UserId).Returns(true);

        if (pointCounts is not null)
        {
            _dataPointRepository
                .GetResolutionPointCountsAsync(DatasetId, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(pointCounts));
        }
    }

    private void SetupOwnedModel(Model? model)
    {
        _modelRepository.UserOwnsModelAsync(ModelId, UserId).Returns(true);
        _modelRepository.GetModelByIdAsync(ModelId, Arg.Any<bool>()).Returns(model);
    }

    // Belirtilmeyen çözünürlükler 0 nokta → izinsiz kabul edilir.
    private static IReadOnlyDictionary<TimeResolution, int> PointCounts(
        int raw = 0, int minute = 0, int hour = 0, int day = 0, int week = 0, int month = 0)
        => new Dictionary<TimeResolution, int>
        {
            [TimeResolution.Raw] = raw,
            [TimeResolution.Minute] = minute,
            [TimeResolution.Hour] = hour,
            [TimeResolution.Day] = day,
            [TimeResolution.Week] = week,
            [TimeResolution.Month] = month
        };

    // Negatif doğrulamalar WithAnyArgs ile: SaveChangesAsync(CancellationToken) gerçek bir token'la çağrılsaydı
    // argümansız DidNotReceive().SaveChangesAsync() onu görmez ve test yanlışlıkla geçerdi.
    private void AssertNothingCreatedOrEnqueued()
    {
        _modelRepository.DidNotReceiveWithAnyArgs().CreateModel(default!);
        _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    private Model GetCreatedModel()
    {
        var call = _modelRepository.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IModelRepository.CreateModel));
        return (Model)call.GetArguments()[0]!;
    }

    // Enqueue<T>() extension method olduğu için doğrudan doğrulanamaz; altta çağrılan
    // IBackgroundJobClient.Create(Job, IState) çağrılarından Job'ları topluyoruz.
    private List<Job> GetEnqueuedJobs()
        => _backgroundJobClient.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IBackgroundJobClient.Create))
            .Select(c => (Job)c.GetArguments()[0]!)
            .ToList();
}