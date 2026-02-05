namespace TrainingApp.Core.Exceptions;

public abstract class DomainException : Exception
{
    public abstract string Code { get; }

    protected DomainException(string message) : base(message) { }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class NotFoundException : DomainException
{
    public override string Code => "NOT_FOUND";
    public string ResourceType { get; }
    public string? ResourceId { get; }

    public NotFoundException(string resourceType, string? resourceId = null)
        : base($"{resourceType} not found{(resourceId is not null ? $": {resourceId}" : "")}")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

public class ValidationException : DomainException
{
    public override string Code => "VALIDATION_ERROR";
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }

    public ValidationException(string field, string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]> { { field, [message] } };
    }
}

public class ConflictException : DomainException
{
    public override string Code => "CONFLICT";

    public ConflictException(string message) : base(message) { }
}
