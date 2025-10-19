namespace TimeSeriesForecaster.Domain.Entities;

public enum JobStatus
{
    Queued,     // Sırada Bekliyor
    Running,    // Çalışıyor
    Completed,  // Başarıyla Tamamlandı
    Failed,     // Hata ile Sonuçlandı
    Cancelled   // İptal Edildi
}

public enum JobType
    {
        ModelTraining,
        DataProcessing,
        BulkPrediction,
        ModelRetraining,
        DataExport,
        AnomalyDetection
    }

public class BackgroundJob
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; } // Navigation property
    public int ProjectId { get; set; }
    public Project? Project { get; set; } // Navigation property
    public int? DatasetId { get; set; }
    public Dataset? Dataset { get; set; }
    public JobType? JobType { get; set; }
    public string? JobData { get; set; }
    public JobStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Progress { get; set; }

}
