using System.Text;
using Dietcode.KnowledgeVault.Application.Abstractions;
using Dietcode.KnowledgeVault.Application.Exceptions;
using Dietcode.KnowledgeVault.Domain.Entities;
using Dietcode.KnowledgeVault.Domain.Exceptions;
using Dietcode.KnowledgeVault.Domain.Policies;
using Dietcode.KnowledgeVault.Domain.ValueObjects;
using Dietcode.KnowledgeVault.Infrastructure.FileSystem;
using Dietcode.KnowledgeVault.Server.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dietcode.KnowledgeVault.IntegrationTests.FileSystem;

public sealed class NoteCreateTests
{
    private static ServiceProvider Provider(TemporaryVault vault, long limit = 2097152)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Vault:RootPath"] = vault.Root,
            ["Vault:MaxFileSizeBytes"] = limit.ToString()
        }).Build();
        return new ServiceCollection().AddVault(config).BuildServiceProvider();
    }

    [Theory]
    [InlineData("# Nota\r\n\r\nLiquidação, ação, 中文, 😀\r\n")]
    [InlineData("")]
    [InlineData("---\ntitle: Nota\n---\n# Conteúdo")]
    public async Task CreatesExactUtf8WithoutBomAndReturnsMetadata(string content)
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault);
        var service = provider.GetRequiredService<IVaultService>();
        var info = await service.CreateAsync("Nota.MD", content);
        Assert.Equal(Encoding.UTF8.GetBytes(content), await File.ReadAllBytesAsync(Path.Combine(vault.Root, "Nota.MD")));
        Assert.Equal(content, (await service.ReadAsync("Nota.MD")).Content);
        Assert.Equal(await service.GetInfoAsync("Nota.MD"), info);
        Assert.Single(await service.ListAsync());
        Assert.Single(Directory.GetFiles(vault.Root));
    }

    [Fact]
    public async Task CreatesInExistingFolderWithNormalizedPath()
    {
        using var vault = new TemporaryVault();
        Directory.CreateDirectory(Path.Combine(vault.Root, "Trabalho"));
        using var provider = Provider(vault);
        var info = await provider.GetRequiredService<IVaultService>().CreateAsync(@"Trabalho\Nota.md", "á");
        Assert.Equal("Trabalho/Nota.md", info.RelativePath);
        Assert.Equal(2, info.Size);
    }

    [Fact]
    public async Task DoesNotOverwriteExistingNote()
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault);
        var service = provider.GetRequiredService<IVaultService>();
        await service.CreateAsync("Nota.md", "original");
        var error = await Assert.ThrowsAsync<NoteCreateException>(() => service.CreateAsync("Nota.md", "replacement"));
        Assert.Equal(NoteCreateError.AlreadyExists, error.Error);
        Assert.Equal("original", (await service.ReadAsync("Nota.md")).Content);
        Assert.Single(Directory.GetFiles(vault.Root));
    }

    [Fact]
    public async Task MissingParentIsNotCreated()
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault);
        var error = await Assert.ThrowsAsync<NoteCreateException>(() =>
            provider.GetRequiredService<IVaultService>().CreateAsync("missing/note.md", "# Note"));
        Assert.Equal(NoteCreateError.ParentNotFound, error.Error);
        Assert.Empty(Directory.EnumerateFileSystemEntries(vault.Root));
    }

    [Theory]
    [InlineData("../outside.md")]
    [InlineData(@"C:\outside.md")]
    [InlineData("%2e%2e/outside.md")]
    [InlineData("note.md:stream")]
    public async Task RejectsUnsafePathsWithoutWriting(string path)
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault);
        await Assert.ThrowsAsync<VaultPathException>(() =>
            provider.GetRequiredService<IVaultService>().CreateAsync(path, "# Note"));
        Assert.Empty(Directory.EnumerateFileSystemEntries(vault.Root));
    }

    [Fact]
    public async Task RejectsForbiddenExtension()
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault);
        var error = await Assert.ThrowsAsync<NoteCreateException>(() =>
            provider.GetRequiredService<IVaultService>().CreateAsync("note.txt", "text"));
        Assert.Equal(NoteCreateError.ExtensionNotAllowed, error.Error);
        Assert.Empty(Directory.GetFiles(vault.Root));
    }

    [Fact]
    public async Task ChecksUtf8ByteLimitBeforeCreatingAnything()
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault, 4);
        var service = provider.GetRequiredService<IVaultService>();
        await Assert.ThrowsAsync<NoteSizeExceededException>(() => service.CreateAsync("bad.md", "ááa"));
        Assert.Empty(Directory.GetFiles(vault.Root));
        Assert.Equal(4, (await service.CreateAsync("good.md", "áá")).Size);
    }

    [Fact]
    public async Task InvalidUnicodeIsRejectedBeforeWriting()
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault);
        var error = await Assert.ThrowsAsync<NoteCreateException>(() =>
            provider.GetRequiredService<IVaultService>().CreateAsync("bad.md", "\uD800"));
        Assert.Equal(NoteCreateError.InvalidEncoding, error.Error);
        Assert.Empty(Directory.GetFiles(vault.Root));
    }

    [Fact]
    public async Task CancelledRequestDoesNotCreateFile()
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.GetRequiredService<IVaultService>().CreateAsync("note.md", "text", cancellation.Token));
        Assert.Empty(Directory.GetFiles(vault.Root));
    }

    [Fact]
    public async Task CancellationBeforePublicationCleansStagingFile()
    {
        using var vault = new TemporaryVault();
        using var cancellation = new CancellationTokenSource();
        var resolver = new InterceptResolver(new VaultPathResolver(vault.Root), 3, cancellation.Cancel);
        var creator = new FileSystemNoteCreator(resolver, new NoteSizePolicy());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            creator.CreateAsync(new Note(new NotePath("note.md"), "# Note"), cancellation.Token));
        Assert.Empty(Directory.GetFiles(vault.Root));
    }

    [Fact]
    public async Task ConcurrentCreationHasOneWinnerAndNoPartialOrTemporaryFiles()
    {
        using var vault = new TemporaryVault();
        using var provider = Provider(vault);
        var service = provider.GetRequiredService<IVaultService>();
        async Task<bool> Attempt(string content)
        {
            try { await service.CreateAsync("note.md", content); return true; }
            catch (NoteCreateException exception) when (exception.Error == NoteCreateError.AlreadyExists) { return false; }
        }
        var outcomes = await Task.WhenAll(Attempt("first"), Attempt("second"));
        Assert.Single(outcomes, success => success);
        Assert.Contains((await service.ReadAsync("note.md")).Content, new[] { "first", "second" });
        Assert.Single(Directory.GetFiles(vault.Root));
    }

    [Fact]
    public async Task DestinationAppearingBeforePublicationIsPreserved()
    {
        using var vault = new TemporaryVault();
        var destination = Path.Combine(vault.Root, "note.md");
        var resolver = new InterceptResolver(new VaultPathResolver(vault.Root), 3,
            () => File.WriteAllText(destination, "external"));
        var creator = new FileSystemNoteCreator(resolver, new NoteSizePolicy());
        var error = await Assert.ThrowsAsync<NoteCreateException>(() =>
            creator.CreateAsync(new Note(new NotePath("note.md"), "replacement"), default));
        Assert.Equal(NoteCreateError.AlreadyExists, error.Error);
        Assert.Equal("external", await File.ReadAllTextAsync(destination));
        Assert.Single(Directory.GetFiles(vault.Root));
    }

    [Fact]
    public async Task JunctionCannotRedirectCreation()
    {
        using var vault = new TemporaryVault();
        vault.CreateDirectoryLink(Path.Combine(vault.Root, "link"), vault.Outside);
        using var provider = Provider(vault);
        await Assert.ThrowsAsync<VaultPathException>(() =>
            provider.GetRequiredService<IVaultService>().CreateAsync("link/note.md", "text"));
        Assert.Empty(Directory.GetFiles(vault.Outside));
    }

    [Fact]
    public async Task JunctionIntroducedAfterResolutionCannotRedirectCreation()
    {
        using var vault = new TemporaryVault();
        var resolver = new InterceptResolver(new VaultPathResolver(vault.Root), 1,
            () => vault.CreateDirectoryLink(Path.Combine(vault.Root, "link"), vault.Outside));
        var creator = new FileSystemNoteCreator(resolver, new NoteSizePolicy());
        await Assert.ThrowsAsync<VaultPathException>(() =>
            creator.CreateAsync(new Note(new NotePath("link/note.md"), "text"), default));
        Assert.Empty(Directory.GetFiles(vault.Outside));
    }

    private sealed class InterceptResolver(IVaultPathResolver inner, int invocation, Action action) : IVaultPathResolver
    {
        private int calls;
        public string Resolve(string? relativePath = null)
        {
            var result = inner.Resolve(relativePath);
            if (++calls == invocation) action();
            return result;
        }
    }
}
