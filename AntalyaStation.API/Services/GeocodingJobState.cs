namespace AntalyaStation.API.Services;
 
// 🟢 Singleton olarak kayıtlı — tüm uygulama boyunca tek bir "geocoding işi" durumu tutar.
// Controller'lar bu nesneyi paylaşarak arka planda çalışan işin ilerlemesini okuyup yazar.
public class GeocodingJobState
{
    public bool IsRunning { get; set; }
    public int TotalPending { get; set; }
    public int Resolved { get; set; }
    public int Failed { get; set; }
    public int Remaining { get; set; }
    public string Message { get; set; } = "Idle.";
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}