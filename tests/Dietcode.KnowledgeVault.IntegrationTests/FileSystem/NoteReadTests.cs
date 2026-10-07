using System.Diagnostics;
using System.Text;
using Dietcode.KnowledgeVault.Application.Abstractions;
using Dietcode.KnowledgeVault.Application.Exceptions;
using Dietcode.KnowledgeVault.Application.Services;
using Dietcode.KnowledgeVault.Domain.Exceptions;
using Dietcode.KnowledgeVault.Infrastructure.FileSystem;
using Dietcode.KnowledgeVault.Server.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dietcode.KnowledgeVault.IntegrationTests.FileSystem;

public sealed class NoteReadTests
{
    private static IVaultService Service(TemporaryVault vault, long limit = 2097152) =>
        CreateService(new VaultPathResolver(vault.Root), limit);

    [Theory]
    [InlineData("# Nota\r\n\r\nLiquidação, ação, café, 中文, 😀\r\n")]
    [InlineData("")]
    [InlineData("---\ncustom: preserve\n---\n# Markdown\n[[Link]]")]
    public async Task ReadsUtf8ExactlyAndReturnsPhysicalMetadata(string content)
    {
        using var vault = new TemporaryVault();
        var folder = Directory.CreateDirectory(Path.Combine(vault.Root, "Trabalho")).FullName;
        var file = Path.Combine(folder, "Nota.MD");
        await File.WriteAllTextAsync(file, content, new UTF8Encoding(false));
        var before = await File.ReadAllBytesAsync(file);
        var expected = new FileInfo(file);
        var service = Service(vault);
        var result = await service.ReadAsync(@"Trabalho\Nota.MD");
        var info = await service.GetInfoAsync("Trabalho/Nota.MD");
        Assert.Equal(content, result.Content);
        Assert.Equal(info, result.Info);
        Assert.Equal("Nota.MD", info.Name);
        Assert.Equal("Trabalho/Nota.MD", info.RelativePath);
        Assert.Equal(".md", info.Extension);
        Assert.Equal(before.Length, info.Size);
        Assert.Equal(new DateTimeOffset(expected.CreationTimeUtc), info.CreatedAt);
        Assert.Equal(new DateTimeOffset(expected.LastWriteTimeUtc), info.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, info.UpdatedAt.Offset);
        Assert.Equal(before, await File.ReadAllBytesAsync(file));
    }

    [Fact]
    public async Task ReadsUtf8BomWithoutReturningBomAsContent()
    {
        using var vault = new TemporaryVault();
        await File.WriteAllTextAsync(Path.Combine(vault.Root, "bom.md"), "ação", new UTF8Encoding(true));
        var result = await Service(vault).ReadAsync("bom.md");
        Assert.Equal("ação", result.Content);
        Assert.Equal(Encoding.UTF8.GetByteCount("ação") + 3, result.Info.Size);
    }

    [Theory]
    [InlineData("missing.md")]
    [InlineData("missing/folder/note.md")]
    public async Task MissingFilesHaveKnownError(string path)
    {
        using var vault = new TemporaryVault();
        var service = Service(vault);
        var read = await Assert.ThrowsAsync<NoteReadException>(() => service.ReadAsync(path));
        var info = await Assert.ThrowsAsync<NoteReadException>(() => service.GetInfoAsync(path));
        Assert.Equal(NoteReadError.NotFound, read.Error);
        Assert.Equal(NoteReadError.NotFound, info.Error);
        Assert.Empty(Directory.EnumerateFileSystemEntries(vault.Root));
    }

    [Theory]
    [InlineData("note.txt")]
    [InlineData("note.json")]
    [InlineData("note")]
    public async Task RejectsForbiddenExtensionBeforeStorage(string path)
    {
        using var vault = new TemporaryVault();
        var service = Service(vault);
        var read = await Assert.ThrowsAsync<NoteReadException>(() => service.ReadAsync(path));
        var info = await Assert.ThrowsAsync<NoteReadException>(() => service.GetInfoAsync(path));
        Assert.Equal(NoteReadError.ExtensionNotAllowed, read.Error);
        Assert.Equal(NoteReadError.ExtensionNotAllowed, info.Error);
    }

    [Theory]
    [InlineData("../outside.md")]
    [InlineData(@"..\outside.md")]
    [InlineData(@"C:\Windows\secret.md")]
    [InlineData(@"\\server\share\note.md")]
    [InlineData("%2e%2e/note.md")]
    [InlineData("note.md:stream")]
    public async Task RejectsUnsafeReadAndInfoPaths(string path)
    {
        using var vault = new TemporaryVault();
        var service = Service(vault);
        await Assert.ThrowsAsync<VaultPathException>(() => service.ReadAsync(path));
        await Assert.ThrowsAsync<VaultPathException>(() => service.GetInfoAsync(path));
    }

    [Fact]
    public async Task AppliesByteLimitToReadAndInfoIncludingBom()
    {
        using var vault = new TemporaryVault();
        await File.WriteAllTextAsync(Path.Combine(vault.Root, "note.md"), "á", new UTF8Encoding(true));
        var service = Service(vault, 4);
        var read = await Assert.ThrowsAsync<NoteSizeExceededException>(() => service.ReadAsync("note.md"));
        var info = await Assert.ThrowsAsync<NoteSizeExceededException>(() => service.GetInfoAsync("note.md"));
        Assert.Equal(5, read.ActualBytes);
        Assert.Equal(4, info.MaximumBytes);
        Assert.Equal("á", (await Service(vault, 5).ReadAsync("note.md")).Content);
    }

    [Fact]
    public async Task SparseOversizedFileIsRejectedBeforeReadingContent()
    {
        using var vault = new TemporaryVault();
        using (var file = File.Create(Path.Combine(vault.Root, "large.md")))
            file.SetLength(2097153);
        await Assert.ThrowsAsync<NoteSizeExceededException>(() => Service(vault).ReadAsync("large.md"));
    }

    [Theory]
    [InlineData(new byte[] { 0xC3, 0x28 })]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x41, 0x00 })]
    public async Task RejectsInvalidUtf8ButInfoDoesNotDecodeContent(byte[] bytes)
    {
        using var vault = new TemporaryVault();
        await File.WriteAllBytesAsync(Path.Combine(vault.Root, "note.md"), bytes);
        var service = Service(vault);
        var error = await Assert.ThrowsAsync<NoteReadException>(() => service.ReadAsync("note.md"));
        Assert.Equal(NoteReadError.InvalidEncoding, error.Error);
        Assert.Equal(bytes.Length, (await service.GetInfoAsync("note.md")).Size);
    }

    [Fact]
    public async Task HonorsCancellationForBothOperations()
    {
        using var vault = new TemporaryVault();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(vault).ReadAsync("note.md", cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(vault).GetInfoAsync("note.md", cancellation.Token));
    }

    [Fact]
    public async Task RejectsDirectoryWithMarkdownName()
    {
        using var vault = new TemporaryVault();
        Directory.CreateDirectory(Path.Combine(vault.Root, "directory.md"));
        var error = await Assert.ThrowsAsync<NoteReadException>(() => Service(vault).ReadAsync("directory.md"));
        Assert.Equal(NoteReadError.AccessDenied, error.Error);
    }

    [Fact]
    public async Task LockedFileReturnsKnownError()
    {
        using var vault = new TemporaryVault();
        var file = Path.Combine(vault.Root, "note.md");
        await File.WriteAllTextAsync(file, "content");
        using var exclusive = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var error = await Assert.ThrowsAsync<NoteReadException>(() => Service(vault).ReadAsync("note.md"));
        Assert.Equal(NoteReadError.Unavailable, error.Error);
    }

    [Fact]
    public async Task JunctionCannotExposeExternalContent()
    {
        using var vault = new TemporaryVault();
        await File.WriteAllTextAsync(Path.Combine(vault.Outside, "note.md"), "outside");
        vault.CreateDirectoryLink(Path.Combine(vault.Root, "link"), vault.Outside);
        await Assert.ThrowsAsync<VaultPathException>(() => Service(vault).ReadAsync("link/note.md"));
        await Assert.ThrowsAsync<VaultPathException>(() => Service(vault).GetInfoAsync("link/note.md"));
    }

    [Fact]
    public async Task DetectsPathRedirectionBetweenResolveAndOpen()
    {
        using var vault = new TemporaryVault();
        await File.WriteAllTextAsync(Path.Combine(vault.Outside, "note.md"), "outside");
        var resolver = new AfterResolve(new VaultPathResolver(vault.Root),
            () => vault.CreateDirectoryLink(Path.Combine(vault.Root, "link"), vault.Outside));
        var service = CreateService(resolver, 1024);
        await Assert.ThrowsAsync<VaultPathException>(() => service.ReadAsync("link/note.md"));
    }

    [Fact]
    public async Task RejectsHardLinkedFiles()
    {
        using var vault = new TemporaryVault();
        var outside = Path.Combine(vault.Outside, "outside.md");
        await File.WriteAllTextAsync(outside, "outside");
        var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in new[] { "/c", "mklink", "/H", Path.Combine(vault.Root, "link.md"), outside })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.True(process.ExitCode == 0, output + error);
        await Assert.ThrowsAsync<VaultPathException>(() => Service(vault).ReadAsync("link.md"));
        await Assert.ThrowsAsync<VaultPathException>(() => Service(vault).GetInfoAsync("link.md"));
    }

    [Fact]
    public async Task HostRegistrationUsesConfiguredReaderAndLimit()
    {
        using var vault = new TemporaryVault();
        await File.WriteAllTextAsync(Path.Combine(vault.Root, "note.md"), "12345", new UTF8Encoding(false));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Vault:RootPath"] = vault.Root,
            ["Vault:MaxFileSizeBytes"] = "4"
        }).Build();
        using var provider = new ServiceCollection().AddVault(configuration).BuildServiceProvider();
        var service = provider.GetRequiredService<IVaultService>();
        await Assert.ThrowsAsync<NoteSizeExceededException>(() => service.ReadAsync("note.md"));
    }

    private static IVaultService CreateService(IVaultPathResolver resolver, long limit)
    {
        var reader = new FileSystemNoteReader(resolver, limit);
        var policy = new Dietcode.KnowledgeVault.Domain.Policies.NoteSizePolicy(limit);
        return new VaultService(reader, new FileSystemNoteLister(resolver, reader),
            new FileSystemNoteCreator(resolver, policy), policy);
    }

    private sealed class AfterResolve(IVaultPathResolver inner, Action action) : IVaultPathResolver
    {
        private bool invoked;
        public string Resolve(string? relativePath = null)
        {
            var path = inner.Resolve(relativePath);
            if (!invoked)
            {
                invoked = true;
                action();
            }
            return path;
        }
    }
}
