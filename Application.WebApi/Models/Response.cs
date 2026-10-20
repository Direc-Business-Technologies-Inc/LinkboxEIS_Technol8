namespace Application.WebApi.Models;

public class Response
{
    public bool Error { get; set; }
    public string Message { get; set; }
    public object? Data { get; set; }
    public string? ResponseError { get; set; }
}
