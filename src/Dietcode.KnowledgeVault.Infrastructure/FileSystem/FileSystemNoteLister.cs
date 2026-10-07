using Dietcode.KnowledgeVault.Application.Abstractions;
using Dietcode.KnowledgeVault.Application.Exceptions;
using Dietcode.KnowledgeVault.Application.Models;
using Dietcode.KnowledgeVault.Domain.ValueObjects;

namespace Dietcode.KnowledgeVault.Infrastructure.FileSystem;

public sealed class FileSystemNoteLister(IVaultPathResolver resolver, INoteReader reader) : INoteLister
{
    public async Task<IReadOnlyCollection<NoteInfo>> ListAsync(VaultPath? folder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var fullPath = resolver.Resolve(folder?.Value);
            if (File.Exists(fullPath))
                throw new NoteListException(NoteListError.NotDirectory, folder?.Value);
            using var lease = WindowsDirectoryLease.Open(fullPath);
            _ = resolver.Resolve(folder?.Value);
            var results = new List<NoteInfo>();
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = false,
                IgnoreInaccessible = false,
                AttributesToSkip = 0,
                ReturnSpecialDirectories = false
            };
            foreach (var entry in new DirectoryInfo(fullPath).EnumerateFileSystemInfos("*", options))
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Skip folders and unsupported extensions; never descend into child directories.
                if ((entry.Attributes & FileAttributes.Directory) != 0 ||
                    !entry.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                    continue;
                var relative = folder is null ? entry.Name : folder.Value + "/" + entry.Name;
                try
                {
                    results.Add(await reader.GetInfoAsync(new NotePath(relative), cancellationToken));
                }
                catch (NoteReadException exception) when (exception.Error == NoteReadError.NotFound)
                {
                    // A note removed after enumeration is no longer part of the listing.
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            WindowsOpenedFileGuard.Validate(lease, fullPath, directory: true);
            _ = resolver.Resolve(folder?.Value);
            return results.OrderBy(info => info.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(info => info.RelativePath, StringComparer.Ordinal).ToArray();
        }
        catch (DirectoryNotFoundException exception)
        {
            throw new NoteListException(NoteListError.FolderNotFound, folder?.Value, exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new NoteListException(NoteListError.AccessDenied, folder?.Value, exception);
        }
        catch (IOException exception)
        {
            throw new NoteListException(NoteListError.Unavailable, folder?.Value, exception);
        }
    }
}
