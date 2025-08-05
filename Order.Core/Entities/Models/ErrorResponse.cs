namespace Order.Core.Entities.Models;

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Title { get; set; } = String.Empty;
    public string Detail { get; set; } = String.Empty;
    public string CorrelationId { get; set; } = String.Empty;
    public DateTime Timestamp { get; set; }
    public string? StackTrace { get; set; }
    public Dictionary<string, object>? Extensions { get; set; }
}