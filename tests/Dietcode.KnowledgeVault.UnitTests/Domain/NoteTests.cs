using Dietcode.KnowledgeVault.Domain.Entities;
using Dietcode.KnowledgeVault.Domain.Exceptions;
using Dietcode.KnowledgeVault.Domain.Models;
using Dietcode.KnowledgeVault.Domain.Policies;
using Dietcode.KnowledgeVault.Domain.ValueObjects;

namespace Dietcode.KnowledgeVault.UnitTests.Domain;

public sealed class NoteTests
{
    [Theory]
    [InlineData("../secret.md")]
    [InlineData("folder/../secret.md")]
    [InlineData("C:\\secret.md")]
    [InlineData("/secret.md")]
    [InlineData("\\\\server\\vault\\note.md")]
    [InlineData("folder//note.md")]
    [InlineData("folder./note.md")]
    [InlineData("folder /note.md")]
    [InlineData("note.md:stream")]
    [InlineData("NUL.md")]
    [InlineData("COM1/note.md")]
    [InlineData("LPT¹.md")]
    [InlineData("note.txt")]
    [InlineData(".md")]
    public void RejectsInvalidNotePaths(string path) =>
        Assert.Throws<ArgumentException>(() => new NotePath(path));

    [Fact]
    public void NormalizesSeparatorsAndPreservesHumanNames()
    {
        var path = new NotePath("02 - Trabalho\\TAG\\Liquidação Retroativa.md");
        Assert.Equal("02 - Trabalho/TAG/Liquidação Retroativa.md", path.Value);
        Assert.Equal(path, new NotePath(path.Value));
    }

    [Fact]
    public void AppendPreservesOriginalAndValidatesCombinedUtf8Size()
    {
        var note = new Note(new NotePath("nota.md"), "á", new NoteSizePolicy(4));
        Assert.Equal("áá", note.AppendContent("á").Content);
        Assert.Equal("á", note.Content);
        Assert.Throws<NoteSizeExceededException>(() => note.AppendContent("áa"));
    }

    [Fact]
    public void ReplacementKeepsSizePolicy()
    {
        var note = new Note(new NotePath("nota.md"), "", new NoteSizePolicy(2));
        Assert.Equal("á", note.ReplaceContent("á").Content);
        Assert.Throws<NoteSizeExceededException>(() => note.ReplaceContent("abc"));
    }

    [Fact]
    public void PreservesRawFrontmatterAndLineEndings()
    {
        const string content = "---\r\ncustom: preserved\r\n---\r\n# Nota\r\n";
        var note = new Note(new NotePath("nota.md"), content);
        Assert.Equal(content + "\r\nDecisão", note.AppendContent("\r\nDecisão").Content);
    }

    [Fact]
    public void VersionComparisonDetectsStaleReads()
    {
        var expected = new NoteVersion(new string('a', 64));
        expected.EnsureMatches(new NoteVersion(new string('A', 64)));
        var actual = new NoteVersion(new string('B', 64));
        var error = Assert.Throws<NoteVersionConflictException>(() => expected.EnsureMatches(actual));
        Assert.Equal(expected, error.Expected);
        Assert.Equal(actual, error.Actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A87F")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    public void RejectsInvalidVersionTokens(string value) =>
        Assert.Throws<ArgumentException>(() => new NoteVersion(value));

    [Fact]
    public void MetadataDefensivelyCopiesTagsAndAllowsOpenTaxonomy()
    {
        var tags = new List<string> { "sql" };
        var metadata = new NoteMetadata(type: "solucao-tecnica", tags: tags);
        tags.Add("tag");
        Assert.Single(metadata.Tags);
        Assert.Equal("solucao-tecnica", metadata.Type);
        Assert.Empty(new NoteMetadata().Tags);
    }

    [Fact]
    public void RejectsInvertedMetadataDates()
    {
        Assert.Throws<ArgumentException>(() => new NoteMetadata(
            created: new DateOnly(2026, 10, 7), updated: new DateOnly(2026, 10, 6)));
    }
}
