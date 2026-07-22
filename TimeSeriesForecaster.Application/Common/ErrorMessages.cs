namespace TimeSeriesForecaster.Application.Common;

public static class ErrorMessages
{
    public const string UserNotFound = "Kullanıcı bulunamadı.";
    public const string UnauthorizedAccess = "Bu işlem için yetkiniz yok.";
    public const string FileCannotBeEmpty = "Dosya boş olamaz.";
    public const string DatasetNotFound = "Dataset bulunamadı.";
    public const string ForecastGenerationFailed = "Tahmin üretimi sırasında bir hata oluştu.";
    public const string InvalidCredentials = "Geçersiz kullanıcı adı veya şifre.";
    public const string ProjectNotFound = "Proje bulunamadı.";
    public const string ForecastNotFound = "Tahmin üretilecek model bulunamadı.";
    public const string ModelNotCompleted = "Tahmin üretebilmek için modelin eğitiminin tamamlanmış olması gerekir.";
    public const string InvalidForecastResponse = "Python API'ından geçerli bir tahmin listesi dönmedi.";
    public const string NotificationNotFound = "Bildirim bulunamadı.";
    public const string ModelNotFound = "Model bulunamadı.";
}
