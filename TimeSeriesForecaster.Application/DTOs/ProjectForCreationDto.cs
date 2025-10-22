using System.ComponentModel.DataAnnotations;

namespace TimeSeriesForecaster.Application.DTOs;

public class ProjectForCreationDto
{
    [Required(ErrorMessage = "Proje adı zorunludur.")]
    [MaxLength(100, ErrorMessage = "Proje adı 100 karakterden uzun olamaz.")]
    public string? Name { get; set; }

    [MaxLength(500, ErrorMessage = "Açıklama 500 karakterden uzun olamaz.")]
    public string? Description { get; set; }
}
