namespace SportsBetting.Exceptions.ExceptionBase;

public abstract class SportsBettingException : Exception
{
    protected SportsBettingException(int statusCode, string title, IReadOnlyList<string> errors)
        : base(errors.FirstOrDefault() ?? title)
    {
        StatusCode = statusCode;
        Title = title;
        Errors = errors;
    }

    public int StatusCode { get; }
    public string Title { get; }
    public IReadOnlyList<string> Errors { get; }
}
