namespace Dietcode.KnowledgeVault.Domain.Exceptions;

public sealed class NoteSizeExceededException(long actualBytes, long maximumBytes)
    : Exception($"Note size {actualBytes} bytes exceeds the limit of {maximumBytes} bytes.")
{
    public long ActualBytes { get; } = actualBytes;
    public long MaximumBytes { get; } = maximumBytes;
}
