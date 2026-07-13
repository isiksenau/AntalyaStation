namespace AntalyaStation.Models // Kendi Namespace'ine göre düzelt
{
    public class PagedResult<T>
    {
        public List<T> Data { get; set; }
        public int TotalCount { get; set; }
    }
}