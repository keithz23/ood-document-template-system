using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Domain.Patterns.Prototype;

namespace DocumentTemplateSystem.UnitTests;

public sealed class PrototypePatternTests
{
    [Fact]
    public void Clone_CreatesIndependentDraftAndRetainsSourceVersion()
    {
        var sourceCreator = Guid.NewGuid();
        var documentCreator = Guid.NewGuid();
        var sourceCreatedAt = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        var documentCreatedAt = sourceCreatedAt.AddDays(1);
        var version = new TemplateVersion(
            Guid.NewGuid(),
            3,
            "<p>Original content</p>",
            ContentFormat.Html,
            sourceCreator,
            sourceCreatedAt);

        var document = version.Clone(
            documentCreator,
            "Independent document",
            documentCreatedAt);

        Assert.NotEqual(version.Id, document.Id);
        Assert.Equal(version.Id, document.TemplateVersionId);
        Assert.Equal(version.Content, document.Content);
        Assert.Equal(documentCreator, document.CreatedBy);
        Assert.Equal(DocumentStatus.Draft, document.Status);
        Assert.Equal(documentCreatedAt, document.CreatedAt);

        document.UpdateDraft(
            "Updated independent document",
            "<p>Changed document content</p>",
            documentCreatedAt.AddHours(1));

        Assert.Equal("<p>Original content</p>", version.Content);
        Assert.Equal("<p>Changed document content</p>", document.Content);
    }

    [Fact]
    public void RequiredPrototypeInterface_CloneCreatesDifferentDocumentInstance()
    {
        var creator = Guid.NewGuid();
        IPrototype<Document> prototype = new TemplateVersion(
            Guid.NewGuid(),
            1,
            "<p>Source</p>",
            ContentFormat.Html,
            creator);

        var first = prototype.Clone();
        var second = prototype.Clone();

        Assert.NotSame(first, second);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.TemplateVersionId, second.TemplateVersionId);
        Assert.Equal("<p>Source</p>", first.Content);
    }

    [Fact]
    public void ClonePlaceholderValues_DoNotMutateSourcePlaceholders()
    {
        var creator = Guid.NewGuid();
        var version = new TemplateVersion(
            Guid.NewGuid(),
            1,
            "<p>{{client_name}}</p>",
            ContentFormat.Html,
            creator);
        var placeholder = version.AddPlaceholder(
            "client_name",
            "Client name",
            PlaceholderDataType.Text,
            true);
        var document = version.Clone(creator, "Client document");

        document.SetPlaceholderValue(placeholder, "Acme Industries");

        Assert.Single(document.PlaceholderValues);
        Assert.Single(version.Placeholders);
        Assert.Equal("client_name", version.Placeholders.Single().Key);
    }
}
