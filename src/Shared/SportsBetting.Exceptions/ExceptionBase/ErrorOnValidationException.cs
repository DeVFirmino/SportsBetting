using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Exceptions.ExceptionBase;

public class ErrorOnValidationException : SportsBettingException
{
    public IList<string> ErrorMessage { get; set; } //Create a list of errors 
    
    public ErrorOnValidationException(IList<string> errors)
    {
        ErrorMessage = errors;
    }
}