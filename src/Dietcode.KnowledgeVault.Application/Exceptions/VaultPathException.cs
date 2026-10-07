namespace Dietcode.KnowledgeVault.Application.Exceptions;

public sealed class VaultPathException(string message, Exception? innerException = null)
    : Exception(message, innerException);