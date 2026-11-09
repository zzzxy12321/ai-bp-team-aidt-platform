namespace AIDT.Domain.Common;

public class AidtDomainException : Exception
{
    public AidtDomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? ErrorCodes.UnexpectedError : errorCode;
    }

    public string ErrorCode { get; }
}

public sealed class ValidationException : AidtDomainException
{
    public ValidationException(string message)
        : base(ErrorCodes.ValidationFailed, message)
    {
    }

    public ValidationException(string errorCode, string message)
        : base(errorCode, message)
    {
    }
}

public sealed class ConflictException(string errorCode, string message)
    : AidtDomainException(errorCode, message);

public sealed class PermissionException(string errorCode, string message)
    : AidtDomainException(errorCode, message);

public sealed class NotFoundException(string message)
    : AidtDomainException(ErrorCodes.ResourceNotFound, message);

public sealed class ServiceUnavailableException(string errorCode, string message)
    : AidtDomainException(errorCode, message);

public sealed class UnsupportedMediaTypeException(string message)
    : AidtDomainException(ErrorCodes.FilePreviewUnsupported, message);

public sealed class MinutesDocumentException(string message)
    : AidtDomainException(ErrorCodes.DocxParseFailed, message);
