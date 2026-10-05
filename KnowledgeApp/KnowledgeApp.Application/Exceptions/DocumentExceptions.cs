namespace KnowledgeApp.Application.Exceptions;

public class DocumentNotFoundException : Exception
{
    public DocumentNotFoundException(string message) : base(message) { }
}

public class DocumentConflictException : Exception
{
    public DocumentConflictException(string message) : base(message) { }
}

public class DocumentValidationException : Exception
{
    public DocumentValidationException(string message) : base(message) { }
}

public class DocumentAccessException : Exception
{
    public DocumentAccessException(string message) : base(message) { }
}

public class StoredFileMissingException : Exception
{
    public StoredFileMissingException(string message) : base(message) { }
}
