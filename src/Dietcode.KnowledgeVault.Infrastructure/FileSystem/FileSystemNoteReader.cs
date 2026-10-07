using System.Text;
using Dietcode.KnowledgeVault.Application.Abstractions;
using Dietcode.KnowledgeVault.Application.Exceptions;
using Dietcode.KnowledgeVault.Application.Models;
using Dietcode.KnowledgeVault.Domain.Exceptions;
using Dietcode.KnowledgeVault.Domain.ValueObjects;

namespace Dietcode.KnowledgeVault.Infrastructure.FileSystem;

public sealed class FileSystemNoteReader : INoteReader
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly IVaultPathResolver resolver;
    private readonly long maximumBytes;

    public FileSystemNoteReader(IVaultPathResolver resolver, long maximumBytes)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        this.resolver = resolver;
        this.maximumBytes = maximumBytes;
    }

    public Task<NoteContent> ReadAsync(NotePath path, CancellationToken cancellationToken) =>
        ExecuteAsync(path, async (stream, info, token) =>
        {
            // Check physical bytes, including a possible UTF-8 BOM, before allocating content.
            using var content = new MemoryStream();
            var buffer = new byte[8192];
            long total = 0;
            int count;
            while ((count = await stream.ReadAsync(buffer, token)) != 0)
            {
                total += count;
                ValidateSize(total);
                await content.WriteAsync(buffer.AsMemory(0, count), token);
            }
            token.ThrowIfCancellationRequested();
            var bytes = content.ToArray();
            var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            var text = Utf8.GetString(bytes, offset, bytes.Length - offset);
            return new NoteContent(info with { Size = total }, text);
        }, cancellationToken);

    public Task<NoteInfo> GetInfoAsync(NotePath path, CancellationToken cancellationToken) =>
        ExecuteAsync(path, (_, info, _) => Task.FromResult(info), cancellationToken);

    private async Task<T> ExecuteAsync<T>(NotePath path,
        Func<FileStream, NoteInfo, CancellationToken, Task<T>> operation, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(path);
        token.ThrowIfCancellationRequested();
        try
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Secure Vault file reads currently require Windows.");
            var fullPath = resolver.Resolve(path.Value);
            await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read,
                FileShare.Read, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
            // A path can change between Resolve and Open. Inspect the opened handle before reading.
            WindowsOpenedFileGuard.Validate(stream.SafeFileHandle, fullPath);
            _ = resolver.Resolve(path.Value);
            token.ThrowIfCancellationRequested();
            ValidateSize(stream.Length);
            var info = new NoteInfo(Path.GetFileName(path.Value), path.Value,
                Path.GetExtension(path.Value).ToLowerInvariant(), stream.Length,
                new DateTimeOffset(File.GetCreationTimeUtc(stream.SafeFileHandle)),
                new DateTimeOffset(File.GetLastWriteTimeUtc(stream.SafeFileHandle)));
            return await operation(stream, info, token);
        }
        catch (FileNotFoundException exception) { throw Failure(NoteReadError.NotFound, path, exception); }
        catch (DirectoryNotFoundException exception) { throw Failure(NoteReadError.NotFound, path, exception); }
        catch (DecoderFallbackException exception) { throw Failure(NoteReadError.InvalidEncoding, path, exception); }
        catch (UnauthorizedAccessException exception) { throw Failure(NoteReadError.AccessDenied, path, exception); }
        catch (IOException exception) { throw Failure(NoteReadError.Unavailable, path, exception); }
    }

    private void ValidateSize(long size)
    {
        if (size > maximumBytes)
            throw new NoteSizeExceededException(size, maximumBytes);
    }

    private static NoteReadException Failure(NoteReadError error, NotePath path, Exception exception) =>
        new(error, path.Value, exception);
}
