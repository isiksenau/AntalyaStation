namespace AntalyaStation.Components.Pages.Models 
{
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
    }
}