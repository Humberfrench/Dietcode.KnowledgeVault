using Dietcode.KnowledgeVault.Application.Exceptions;
using Dietcode.KnowledgeVault.Infrastructure.FileSystem;

namespace Dietcode.KnowledgeVault.IntegrationTests.FileSystem;

public sealed class VaultPathResolverTests
{
    [Theory]
    [InlineData("../outside.md")]
    [InlineData(@"..\outside.md")]
    [InlineData("folder/../../outside.md")]
    [InlineData("folder/../note.md")]
    [InlineData("C:/Windows/win.ini")]
    [InlineData(@"C:\Windows\win.ini")]
    [InlineData("C:note.md")]
    [InlineData(@"\\server\share\note.md")]
    [InlineData("/etc/passwd")]
    [InlineData(@"\\?\C:\Windows\win.ini")]
    [InlineData("folder//note.md")]
    [InlineData("note.md:stream")]
    [InlineData("NUL.md")]
    [InlineData("folder./note.md")]
    [InlineData("folder /note.md")]
    [InlineData("%2e%2e%2foutside.md")]
    [InlineData("%252e%252e%255coutside.md")]
    [InlineData("folder/%2Foutside.md")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData("folder/\0note.md")]
    public void RejectsUnsafePaths(string path)
    {
        using var vault = new TemporaryVault();
        var resolver = new VaultPathResolver(vault.Root);
        Assert.Throws<VaultPathException>(() => resolver.Resolve(path));
    }

    [Theory]
    [InlineData("02 - Trabalho/TAG/URs Vencidas.md")]
    [InlineData(@"02 - Trabalho\TAG\URs Vencidas.md")]
    [InlineData("01 - Diario/2026/2026-10-07.md")]
    [InlineData("folder/report..draft.md")]
    public void ResolvesLiteralNamesWithoutCreatingFiles(string path)
    {
        using var vault = new TemporaryVault();
        var result = new VaultPathResolver(vault.Root).Resolve(path);
        Assert.Equal(Path.GetFullPath(Path.Combine(vault.Root,
            path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))), result);
        Assert.Empty(Directory.EnumerateFileSystemEntries(vault.Root));
    }

    [Fact]
    public void ResolvesRootWithTrailingSeparator()
    {
        using var vault = new TemporaryVault();
        Assert.Equal(vault.Root, new VaultPathResolver(vault.Root + Path.DirectorySeparatorChar).Resolve());
    }

    [Fact]
    public void RejectsSiblingDirectoryWithSamePrefix()
    {
        using var vault = new TemporaryVault();
        var resolver = new VaultPathResolver(vault.Root);
        Assert.Throws<VaultPathException>(() => resolver.Resolve("../Vault-backup/note.md"));
        Assert.Throws<VaultPathException>(() => resolver.Resolve(Path.Combine(vault.Outside, "note.md")));
    }

    [Fact]
    public void AllowsExistingFileButRejectsItAsParent()
    {
        using var vault = new TemporaryVault();
        var file = Path.Combine(vault.Root, "note.md");
        File.WriteAllText(file, "# Teste");
        var resolver = new VaultPathResolver(vault.Root);
        Assert.Equal(file, resolver.Resolve("note.md"));
        Assert.Throws<VaultPathException>(() => resolver.Resolve("note.md/child.md"));
    }

    [Fact]
    public void RejectsLinkIntroducedAfterConstruction()
    {
        using var vault = new TemporaryVault();
        var resolver = new VaultPathResolver(vault.Root);
        vault.CreateDirectoryLink(Path.Combine(vault.Root, "link"), vault.Outside);
        Assert.Throws<VaultPathException>(() => resolver.Resolve("link/note.md"));
        Assert.Throws<VaultPathException>(() => resolver.Resolve("link"));
    }

    [Fact]
    public void RejectsLinksEvenWhenTargetIsInsideVault()
    {
        using var vault = new TemporaryVault();
        var target = Directory.CreateDirectory(Path.Combine(vault.Root, "real")).FullName;
        vault.CreateDirectoryLink(Path.Combine(vault.Root, "link"), target);
        Assert.Throws<VaultPathException>(() => new VaultPathResolver(vault.Root).Resolve("link/note.md"));
    }

    [Fact]
    public void RejectsRootAndAncestorJunctions()
    {
        using var vault = new TemporaryVault();
        Directory.CreateDirectory(Path.Combine(vault.Outside, "child"));
        var link = Path.Combine(vault.Parent, "linked-root");
        vault.CreateDirectoryLink(link, vault.Outside);
        Assert.Throws<VaultPathException>(() => new VaultPathResolver(link));
        Assert.Throws<VaultPathException>(() => new VaultPathResolver(Path.Combine(link, "child")));
    }

    [Fact]
    public void RejectsDanglingLinks()
    {
        using var vault = new TemporaryVault();
        var target = Directory.CreateDirectory(Path.Combine(vault.Outside, "target")).FullName;
        vault.CreateDirectoryLink(Path.Combine(vault.Root, "dangling"), target);
        Directory.Delete(target);
        Assert.Throws<VaultPathException>(() => new VaultPathResolver(vault.Root).Resolve("dangling/note.md"));
    }

    [Fact]
    public void RevalidatesRootAfterReplacementWithLink()
    {
        using var vault = new TemporaryVault();
        var resolver = new VaultPathResolver(vault.Root);
        Directory.Delete(vault.Root);
        vault.CreateDirectoryLink(vault.Root, vault.Outside);
        Assert.Throws<VaultPathException>(() => resolver.Resolve());
    }

    [Fact]
    public void DoesNotCreateMissingRoot()
    {
        using var vault = new TemporaryVault();
        var missing = Path.Combine(vault.Parent, "missing");
        Assert.Throws<VaultPathException>(() => new VaultPathResolver(missing));
        Assert.False(Directory.Exists(missing));
    }
}
