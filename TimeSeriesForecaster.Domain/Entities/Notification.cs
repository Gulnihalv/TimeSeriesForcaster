namespace TimeSeriesForecaster.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; } // Navigation property
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    // "Model" veya "Dataset" - frontend ileride ilgili sayfaya yönlendirmek isterse diye
    public string RelatedEntityType { get; set; } = string.Empty;
    public int RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}