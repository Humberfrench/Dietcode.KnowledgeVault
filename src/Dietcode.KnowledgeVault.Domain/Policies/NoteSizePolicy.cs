using System.Text;
using Dietcode.KnowledgeVault.Domain.Exceptions;

namespace Dietcode.KnowledgeVault.Domain.Policies;

public sealed class NoteSizePolicy
{
    public const long DefaultMaximumBytes = 2 * 1024 * 1024;
    private static readonly UTF8Encoding Encoding = new(false, true);
    public long MaximumBytes { get; }

    public NoteSizePolicy(long maximumBytes = DefaultMaximumBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        MaximumBytes = maximumBytes;
    }

    public void Validate(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var bytes = Encoding.GetByteCount(content);
        if (bytes > MaximumBytes)
            throw new NoteSizeExceededException(bytes, MaximumBytes);
    }
}
