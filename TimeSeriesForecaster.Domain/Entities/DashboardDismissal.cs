namespace TimeSeriesForecaster.Domain.Entities;

public class DashboardDismissal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; } // Navigation property
    // "Model" veya "Dataset" - Notification.RelatedEntityType ile aynı konvansiyon
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public DateTime DismissedAt { get; set; }
}
