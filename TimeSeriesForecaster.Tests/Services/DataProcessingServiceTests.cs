using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TimeSeriesForecaster.Application.Common;
using TimeSeriesForecaster.Application.Contracts.Application;
using TimeSeriesForecaster.Application.Contracts.Persistence;
using TimeSeriesForecaster.Application.Services;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Tests.Services;

public class DataProcessingServiceTests : IDisposable
{
    private const int DatasetId = 5;
    private const int ProjectId = 2;
    private const int OwnerUserId = 99;
    private const string CsvFileName = "data.csv";

    private readonly IDataPointRepository _dataPointRepository = Substitute.For<IDataPointRepository>();
    private readonly IDatasetRepository _datasetRepository = Substitute.For<IDatasetRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IWebHostEnvironment _env = Substitute.For<IWebHostEnvironment>();

    private readonly string _tempDir;
    private readonly Dataset _dataset;

    // BulkCopyDataPointsAsync'e verilen liste servis tarafından hemen Clear() ediliyor;
    // bu yüzden argümanı çağrı anında kopyalıyoruz, sonradan ReceivedCalls'a bakmak boş liste görürdü.
    private readonly List<DataPoint> _writtenPoints = new();
    private readonly List<int> _chunkSizes = new();

    public DataProcessingServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tsf-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _env.ContentRootPath.Returns(_tempDir);

        _dataset = new Dataset
        {
            Id = DatasetId,
            ProjectId = ProjectId,
            Name = "Test Dataset",
            FilePath = CsvFileName,
            DateColumn = "Date",
            TargetColumn = "Value"
        };
        _datasetRepository.GetDatasetByIdAsync(DatasetId, Arg.Any<bool>()).Returns(_dataset);
        _projectRepository.GetProjectByIdAsync(ProjectId, Arg.Any<bool>()).Returns(new Project { Id = ProjectId, UserId = OwnerUserId });

        // Result sınıfı interface/pure virtual olmadığı için NSubstitute auto-value olarak null döner;
        // servis .IsSuccess'e eriştiğinde NullReferenceException alırdık.
        _notificationService
            .CreateNotificationAsync(Arg.Any<int>(), Arg.Any<NotificationType>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(Result.Success());

        _dataPointRepository
            .When(r => r.BulkCopyDataPointsAsync(Arg.Any<IEnumerable<DataPoint>>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var chunk = call.Arg<IEnumerable<DataPoint>>().ToList();
                _chunkSizes.Add(chunk.Count);
                _writtenPoints.AddRange(chunk);
            });
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    // ---------- Geçerli CSV ----------

    [Fact]
    public async Task ProcessDatasetAsync_ValidCsv_ImportsAllRowsAndMarksDatasetCompleted()
    {
        // Tarihler bilerek sıralı değil: Start/End ilk/son satırdan değil min/max'tan gelmeli.
        WriteCsv("""
            Date,Value
            2024-01-03,30.5
            2024-01-01,10
            2024-01-05,50.25
            2024-01-02,20
            2024-01-04,40
            """);

        var result = await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, _writtenPoints.Count);
        Assert.True(_dataset.IsProcessed);
        Assert.Equal(ProcessingStatus.Completed, _dataset.Status);
        Assert.Equal(100, _dataset.ProgressPercentage);
        Assert.Equal(5, _dataset.RecordCount);
        Assert.Equal(new DateTime(2024, 1, 1), _dataset.StartDate);
        Assert.Equal(new DateTime(2024, 1, 5), _dataset.EndDate);
        Assert.Null(_dataset.SkippedRowCount);
        Assert.Null(_dataset.ErrorMessage);
    }

    [Fact]
    public async Task ProcessDatasetAsync_ValidCsv_WritesTimestampsAndValuesFromFile()
    {
        WriteCsv("""
            Date,Value
            2024-01-01,10.5
            2024-01-02,-3
            """);

        await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.Collection(_writtenPoints.OrderBy(p => p.Timestamp),
            p =>
            {
                Assert.Equal(new DateTime(2024, 1, 1), p.Timestamp);
                Assert.Equal(10.5m, p.Value);
                Assert.Equal(DatasetId, p.DatasetId);
                Assert.False(p.IsOutlier);
            },
            p =>
            {
                Assert.Equal(new DateTime(2024, 1, 2), p.Timestamp);
                Assert.Equal(-3m, p.Value);
            });
    }

    [Fact]
    public async Task ProcessDatasetAsync_ValidCsv_SendsSuccessNotificationToProjectOwner()
    {
        WriteCsv("""
            Date,Value
            2024-01-01,1
            """);

        await CreateSut().ProcessDatasetAsync(DatasetId);

        await _notificationService.Received(1).CreateNotificationAsync(
            OwnerUserId, NotificationType.DatasetProcessingCompleted, Arg.Any<string>(), "Dataset", DatasetId);
    }

    [Fact]
    public async Task ProcessDatasetAsync_ColumnNameCaseDiffers_StillMatchesColumns()
    {
        // Yükleme sırasındaki kolon doğrulaması (DatasetService) büyük/küçük harf duyarsız; işleme de öyle olmalı,
        // yoksa doğrulamadan geçen bir dataset işlenirken "kolon bulunamadı" hatasına düşerdi.
        _dataset.DateColumn = "date";
        _dataset.TargetColumn = "VALUE";
        WriteCsv("""
            Date,Value
            2024-01-01,1
            """);

        var result = await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.True(result.IsSuccess);
        Assert.Single(_writtenPoints);
    }

    // ---------- UTC ve saat dilimi ----------

    [Fact]
    public async Task ProcessDatasetAsync_ValidCsv_AllTimestampsHaveUtcKind()
    {
        // Npgsql, "timestamp with time zone" kolonuna Kind != Utc bir DateTime yazmayı reddediyor.
        WriteCsv("""
            Date,Value
            2024-01-01,1
            2024-01-02 10:30:00,2
            2024/01/03,3
            """);

        await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.Equal(3, _writtenPoints.Count);
        Assert.All(_writtenPoints, p => Assert.Equal(DateTimeKind.Utc, p.Timestamp.Kind));
    }

    [Fact]
    public async Task ProcessDatasetAsync_ValidCsv_DatasetStartAndEndDatesHaveUtcKind()
    {
        WriteCsv("""
            Date,Value
            2024-01-01,1
            2024-01-02,2
            """);

        await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.Equal(DateTimeKind.Utc, _dataset.StartDate!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, _dataset.EndDate!.Value.Kind);
    }

    // DİKKAT: Bu test, bug olsa bile UTC saat diliminde çalışan bir makinede GEÇER (orada yerel saat = UTC,
    // kayma oluşmaz). Asıl korumayı sağlaması için UTC olmayan bir ortamda çalıştırılmalı:
    //   TZ=Europe/Istanbul dotnet test
    // CI'da da test adımına aynı ortam değişkeni verilmeli.
    [Theory]
    [InlineData("2024-01-01T00:00:00Z", 2024, 1, 1, 0)]
    [InlineData("2024-01-01T00:00:00+03:00", 2023, 12, 31, 21)]
    [InlineData("2024-01-01T05:00:00-02:00", 2024, 1, 1, 7)]
    public async Task ProcessDatasetAsync_TimestampWithOffset_IsConvertedToUtcNotRelabeled(
        string input, int year, int month, int day, int hour)
    {
        WriteCsv($"Date,Value\n{input},1\n");

        await CreateSut().ProcessDatasetAsync(DatasetId);

        var point = Assert.Single(_writtenPoints);
        // Kind'a değil DEĞERE bakıyoruz: kayma bug'ı Kind'ı doğru (Utc) bırakıp saati 3 saat kaydırıyordu.
        Assert.Equal(new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Utc), point.Timestamp);
        Assert.Equal(DateTimeKind.Utc, point.Timestamp.Kind);
    }

    // ---------- İdempotency ----------

    [Fact]
    public async Task ProcessDatasetAsync_ValidCsv_RemovesExistingPointsBeforeWritingNewOnes()
    {
        WriteCsv("""
            Date,Value
            2024-01-01,1
            2024-01-02,2
            """);

        await CreateSut().ProcessDatasetAsync(DatasetId);

        Received.InOrder(() =>
        {
            _dataPointRepository.RemoveDataPointsForDatasetAsync(DatasetId, Arg.Any<CancellationToken>());
            _dataPointRepository.BulkCopyDataPointsAsync(Arg.Any<IEnumerable<DataPoint>>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task ProcessDatasetAsync_InvalidColumns_StillRemovesExistingPoints()
    {
        // Yeniden işleme başarısız olsa bile eski noktalar silinmiş olmalı; yarım/eski veri kalmamalı.
        _dataset.DateColumn = "NoSuchColumn";
        WriteCsv("""
            Date,Value
            2024-01-01,1
            """);

        await CreateSut().ProcessDatasetAsync(DatasetId);

        await _dataPointRepository.Received(1).RemoveDataPointsForDatasetAsync(DatasetId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDatasetAsync_ReprocessingDataset_ResetsPreviousResults()
    {
        _dataset.RecordCount = 999;
        _dataset.SkippedRowCount = 3;
        _dataset.ErrorMessage = "önceki hata";
        _dataset.StartDate = new DateTime(1999, 1, 1);
        _dataset.EndDate = new DateTime(2000, 1, 1);
        WriteCsv("""
            Date,Value
            2024-01-01,1
            2024-01-02,2
            """);

        await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.Equal(2, _dataset.RecordCount);
        Assert.Null(_dataset.SkippedRowCount);
        Assert.Null(_dataset.ErrorMessage);
        Assert.Equal(new DateTime(2024, 1, 1), _dataset.StartDate);
        Assert.Equal(new DateTime(2024, 1, 2), _dataset.EndDate);
    }

    // ---------- Bozuk kolon adları ----------

    [Theory]
    [InlineData("NoSuchColumn", "Value")]
    [InlineData("Date", "NoSuchColumn")]
    [InlineData("NoSuchDate", "NoSuchValue")]
    public async Task ProcessDatasetAsync_InvalidColumnNames_FailsWithoutWritingAnyPoints(string dateColumn, string targetColumn)
    {
        _dataset.DateColumn = dateColumn;
        _dataset.TargetColumn = targetColumn;
        WriteCsv("""
            Date,Value
            2024-01-01,1
            2024-01-02,2
            """);

        var result = await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
        Assert.False(_dataset.IsProcessed);
        Assert.Equal(ProcessingStatus.Failed, _dataset.Status);
        Assert.False(string.IsNullOrWhiteSpace(_dataset.ErrorMessage));
        await _dataPointRepository.DidNotReceiveWithAnyArgs().BulkCopyDataPointsAsync(default!, default);
    }

    [Fact]
    public async Task ProcessDatasetAsync_InvalidColumnNames_SendsFailureNotification()
    {
        _dataset.DateColumn = "NoSuchColumn";
        WriteCsv("""
            Date,Value
            2024-01-01,1
            """);

        await CreateSut().ProcessDatasetAsync(DatasetId);

        await _notificationService.Received(1).CreateNotificationAsync(
            OwnerUserId, NotificationType.DatasetProcessingFailed, Arg.Any<string>(), "Dataset", DatasetId);
    }

    // ---------- Kısmi bozuk satırlar ----------

    [Fact]
    public async Task ProcessDatasetAsync_SomeRowsMalformed_WritesValidRowsAndCountsSkippedOnes()
    {
        WriteCsv("""
            Date,Value
            2024-01-01,10
            not-a-date,20
            2024-01-03,abc
            2024-01-04,40
            2024-01-05,50
            """);

        var result = await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, _writtenPoints.Count);
        Assert.Equal(new[] { 10m, 40m, 50m }, _writtenPoints.Select(p => p.Value).OrderBy(v => v));
        Assert.Equal(2, _dataset.SkippedRowCount);
        Assert.Equal(3, _dataset.RecordCount);
        Assert.True(_dataset.IsProcessed);
        Assert.Equal(ProcessingStatus.Completed, _dataset.Status);
    }

    [Fact]
    public async Task ProcessDatasetAsync_RowWithMissingField_CountedAsSkipped()
    {
        // Kolon sayısı eksik (kısa) satırlar exception fırlatmamalı, atlanan satır sayılmalı.
        WriteCsv("""
            Date,Value
            2024-01-01,1
            2024-01-02
            2024-01-03,3
            """);

        var result = await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _writtenPoints.Count);
        Assert.Equal(1, _dataset.SkippedRowCount);
    }

    [Fact]
    public async Task ProcessDatasetAsync_SomeRowsMalformed_StartAndEndDatesIgnoreSkippedRows()
    {
        // Değeri bozuk olan satırın (2023-12-31) tarihi geçerli olsa bile aralığa dahil edilmemeli.
        WriteCsv("""
            Date,Value
            2023-12-31,abc
            2024-01-02,1
            2024-01-03,2
            2024-02-01,xyz
            """);

        await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.Equal(new DateTime(2024, 1, 2), _dataset.StartDate);
        Assert.Equal(new DateTime(2024, 1, 3), _dataset.EndDate);
        Assert.Equal(2, _dataset.SkippedRowCount);
    }

    [Fact]
    public async Task ProcessDatasetAsync_AllRowsMalformed_FailsWithoutWritingAnyPoints()
    {
        WriteCsv("""
            Date,Value
            garbage,1
            2024-01-02,garbage
            """);

        var result = await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
        Assert.False(_dataset.IsProcessed);
        await _dataPointRepository.DidNotReceiveWithAnyArgs().BulkCopyDataPointsAsync(default!, default);
    }

    // ---------- Chunk'lama ----------

    [Fact]
    public async Task ProcessDatasetAsync_MoreRowsThanChunkSize_CallsBulkCopyMultipleTimes()
    {
        WriteCsv(GenerateCsv(rowCount: 5));

        var result = await CreateSut(chunkSize: 2).ProcessDatasetAsync(DatasetId);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 2, 2, 1 }, _chunkSizes);
        Assert.Equal(5, _writtenPoints.Count);
        Assert.Equal(5, _dataset.RecordCount);
    }

    [Fact]
    public async Task ProcessDatasetAsync_RowCountIsExactMultipleOfChunkSize_DoesNotWriteEmptyChunk()
    {
        WriteCsv(GenerateCsv(rowCount: 4));

        await CreateSut(chunkSize: 2).ProcessDatasetAsync(DatasetId);

        Assert.Equal(new[] { 2, 2 }, _chunkSizes);
    }

    [Fact]
    public async Task ProcessDatasetAsync_FewerRowsThanChunkSize_CallsBulkCopyOnce()
    {
        WriteCsv(GenerateCsv(rowCount: 3));

        await CreateSut(chunkSize: 100).ProcessDatasetAsync(DatasetId);

        Assert.Equal(new[] { 3 }, _chunkSizes);
    }

    [Fact]
    public async Task ProcessDatasetAsync_MultipleChunks_RemovesExistingPointsOnlyBeforeFirstWrite()
    {
        WriteCsv(GenerateCsv(rowCount: 5));

        await CreateSut(chunkSize: 2).ProcessDatasetAsync(DatasetId);

        Received.InOrder(() =>
        {
            _dataPointRepository.RemoveDataPointsForDatasetAsync(DatasetId, Arg.Any<CancellationToken>());
            _dataPointRepository.BulkCopyDataPointsAsync(Arg.Any<IEnumerable<DataPoint>>(), Arg.Any<CancellationToken>());
            _dataPointRepository.BulkCopyDataPointsAsync(Arg.Any<IEnumerable<DataPoint>>(), Arg.Any<CancellationToken>());
            _dataPointRepository.BulkCopyDataPointsAsync(Arg.Any<IEnumerable<DataPoint>>(), Arg.Any<CancellationToken>());
        });
    }

    // ---------- Altyapı hataları ve iptal ----------

    [Fact]
    public async Task ProcessDatasetAsync_BulkCopyFails_FailsJobWithoutCountingRowsAsSkipped()
    {
        _dataPointRepository
            .BulkCopyDataPointsAsync(Arg.Any<IEnumerable<DataPoint>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB bağlantısı koptu")));
        WriteCsv(GenerateCsv(rowCount: 5));

        var result = await CreateSut(chunkSize: 2).ProcessDatasetAsync(DatasetId);

        Assert.Equal(ResultErrorType.Unexpected, result.ErrorType);
        Assert.Equal(ProcessingStatus.Failed, _dataset.Status);
        Assert.False(_dataset.IsProcessed);
        // Altyapı hatası "bozuk satır" değildir.
        Assert.Null(_dataset.SkippedRowCount);
        // İlk flush hatası anında yukarı fırlamalı. Eski kodda hata yutuluyor, liste chunk boyutunu aşıp
        // büyümeye devam ediyor ve döngü sonunda ikinci kez deneniyordu (Received(2)).
        await _dataPointRepository.Received(1).BulkCopyDataPointsAsync(Arg.Any<IEnumerable<DataPoint>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDatasetAsync_Cancelled_ThrowsAndDoesNotCompleteDataset()
    {
        WriteCsv(GenerateCsv(rowCount: 5));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // İptal yutulmamalı: yeniden fırlatılmazsa Hangfire job'ı "başarılı" sayar.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateSut().ProcessDatasetAsync(DatasetId, cts.Token));

        Assert.NotEqual(ProcessingStatus.Completed, _dataset.Status);
        Assert.False(_dataset.IsProcessed);
        Assert.Null(_dataset.SkippedRowCount);
        // Son durum iptal edilmiş token'la değil CancellationToken.None ile kaydedilmeli; aksi halde gerçek
        // EF Core'da kayıt hiç yapılmaz ve dataset "Processing"de asılı kalır.
        await _unitOfWork.Received().SaveChangesAsync(CancellationToken.None);
    }

    // ---------- Diğer hata yolları ----------

    [Fact]
    public async Task ProcessDatasetAsync_DatasetNotFound_ReturnsNotFoundAndTouchesNothing()
    {
        _datasetRepository.GetDatasetByIdAsync(123, Arg.Any<bool>()).Returns((Dataset?)null);

        var result = await CreateSut().ProcessDatasetAsync(123);

        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
        await _dataPointRepository.DidNotReceiveWithAnyArgs().RemoveDataPointsForDatasetAsync(default, default);
        await _dataPointRepository.DidNotReceiveWithAnyArgs().BulkCopyDataPointsAsync(default!, default);
    }

    [Fact]
    public async Task ProcessDatasetAsync_FileMissing_MarksDatasetFailedInsteadOfLeavingItProcessing()
    {
        // CSV hiç yazılmadı: dosya I/O hatası dataset'i "Processing"de asılı bırakmamalı.
        var result = await CreateSut().ProcessDatasetAsync(DatasetId);

        Assert.Equal(ResultErrorType.Unexpected, result.ErrorType);
        Assert.Equal(ProcessingStatus.Failed, _dataset.Status);
        Assert.False(_dataset.IsProcessed);
        Assert.False(string.IsNullOrWhiteSpace(_dataset.ErrorMessage));
    }

    // ---------- Yardımcılar ----------

    private DataProcessingService CreateSut(int chunkSize = DataProcessingService.DefaultChunkSize)
        => new(_dataPointRepository, _datasetRepository, _projectRepository, _notificationService, _unitOfWork, _env,
            NullLogger<DataProcessingService>.Instance, chunkSize);

    private void WriteCsv(string content)
        => File.WriteAllText(Path.Combine(_tempDir, CsvFileName), content, new UTF8Encoding(false));

    private static string GenerateCsv(int rowCount)
    {
        var sb = new StringBuilder("Date,Value\n");
        for (var i = 0; i < rowCount; i++)
        {
            sb.Append(new DateTime(2024, 1, 1).AddDays(i).ToString("yyyy-MM-dd")).Append(',').Append(i + 1).Append('\n');
        }
        return sb.ToString();
    }
}