namespace TaskManagement.Application.Interfaces
{
    public interface IHtmlSanitizer
    {
        string Sanitize(string? input);
    }
}
