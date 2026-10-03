using Microsoft.VisualBasic;

namespace CarePlus.API.Extension;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string messaege) : base(messaege)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> Errors) : base("One or more validation errors occurred.")
    {
        this.Errors = Errors;
    }
}