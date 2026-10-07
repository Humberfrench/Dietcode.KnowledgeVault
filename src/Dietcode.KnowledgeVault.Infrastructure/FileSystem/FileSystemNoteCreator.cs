using System.Text;
using Dietcode.KnowledgeVault.Application.Abstractions;
using Dietcode.KnowledgeVault.Application.Exceptions;
using Dietcode.KnowledgeVault.Application.Models;
using Dietcode.KnowledgeVault.Domain.Entities;
using Dietcode.KnowledgeVault.Domain.Policies;

namespace Dietcode.KnowledgeVault.Infrastructure.FileSystem;

public sealed class FileSystemNoteCreator(IVaultPathResolver resolver, NoteSizePolicy sizePolicy) : INoteCreator
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public async Task<NoteInfo> CreateAsync(Note note, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(note);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            sizePolicy.Validate(note.Content);
            var bytes = Utf8.GetBytes(note.Content);
            var destination = resolver.Resolve(note.Path.Value);
            var parent = Path.GetDirectoryName(destination)!;
            using var directories = WindowsDirectoryChainLease.Open(parent);
            _ = resolver.Resolve(note.Path.Value);
            if (File.Exists(destination) || Directory.Exists(destination))
                throw new NoteCreateException(NoteCreateError.AlreadyExists, note.Path.Value);

            string? temporary = null;
            try
            {
                var candidate = Path.Combine(parent, ".knowledgevault-" + Guid.NewGuid().ToString("N") + ".tmp");
                await using (var stream = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write,
                                 FileShare.None, 8192, FileOptions.Asynchronous))
                {
                    temporary = candidate; // Clean up only a file this invocation successfully created.
                    WindowsOpenedFileGuard.Validate(stream.SafeFileHandle, candidate);
                    await stream.WriteAsync(bytes, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }
                var staged = new FileInfo(temporary);
                var info = new NoteInfo(Path.GetFileName(note.Path.Value), note.Path.Value, ".md",
                    staged.Length, new DateTimeOffset(staged.CreationTimeUtc),
                    new DateTimeOffset(staged.LastWriteTimeUtc));
                _ = resolver.Resolve(note.Path.Value);
                cancellationToken.ThrowIfCancellationRequested();
                // Same-directory rename: publish only complete content, without replacing an existing note.
                File.Move(temporary, destination, overwrite: false);
                temporary = null;
                // Publication is the commit point; cancellation after it does not undo a successful create.
                return info;
            }
            finally
            {
                if (temporary is not null)
                    File.Delete(temporary);
            }
        }
        catch (EncoderFallbackException exception) { throw Failure(NoteCreateError.InvalidEncoding, note, exception); }
        catch (DirectoryNotFoundException exception) { throw Failure(NoteCreateError.ParentNotFound, note, exception); }
        catch (UnauthorizedAccessException exception) { throw Failure(NoteCreateError.AccessDenied, note, exception); }
        catch (IOException exception) when ((exception.HResult & 0xFFFF) is 80 or 183)
        {
            throw Failure(NoteCreateError.AlreadyExists, note, exception);
        }
        catch (IOException exception) { throw Failure(NoteCreateError.Unavailable, note, exception); }
    }

    private static NoteCreateException Failure(NoteCreateError error, Note note, Exception exception) =>
        new(error, note.Path.Value, exception);
}
