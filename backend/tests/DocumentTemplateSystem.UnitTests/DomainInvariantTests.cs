using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.UnitTests;

public sealed class DomainInvariantTests
{
    [Fact]
    public void NewTemplate_CreatesInitialDraftVersion()
    {
        var template = CreateTemplate();

        var initialVersion = Assert.Single(template.Versions);
        Assert.Equal(1, initialVersion.VersionNumber);
        Assert.Equal(VersionStatus.Draft, initialVersion.Status);
        Assert.False(initialVersion.IsCurrent);
    }

    [Fact]
    public void DraftTemplate_MetadataCanBeUpdatedButPublishedWorkflowStateCannot()
    {
        var creator = Guid.NewGuid();
        var template = CreateTemplate(creator);
        var newCategoryId = Guid.NewGuid();

        template.UpdateDraftMetadata("  Revised agreement  ", newCategoryId);

        Assert.Equal("Revised agreement", template.Name);
        Assert.Equal(newCategoryId, template.CategoryId);

        var version = template.Versions.Single();
        version.Publish(creator);
        template.SetCurrentVersion(version.Id);
        template.Activate();
        Assert.Throws<InvalidOperationException>(() =>
            template.UpdateDraftMetadata("Another name", Guid.NewGuid()));
    }

    [Fact]
    public void Category_RenamePreservesActivationState()
    {
        var category = new Category("Business", Guid.NewGuid());
        category.Deactivate();

        category.Rename("  Legal  ");

        Assert.Equal("Legal", category.Name);
        Assert.False(category.IsActive);
    }

    [Fact]
    public void User_RoleChangePreservesIdentityAndActivityState()
    {
        var user = new User(
            "author",
            "password-hash",
            "Document Author",
            "author@example.test",
            UserRole.User);
        var id = user.Id;

        user.ChangeRole(UserRole.Admin);

        Assert.Equal(id, user.Id);
        Assert.True(user.IsActive);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public void PublishedVersion_CannotBeEditedInPlace()
    {
        var template = CreateTemplate();
        var version = template.Versions.Single();
        version.Publish(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            version.UpdateContent("<p>Changed</p>", ContentFormat.Html));
        Assert.Throws<InvalidOperationException>(() =>
            version.AddPlaceholder(
                "new_key",
                "New key",
                PlaceholderDataType.Text,
                false));
    }

    [Fact]
    public void DraftVersion_CannotBecomeCurrent()
    {
        var template = CreateTemplate();
        var draft = template.Versions.Single();

        Assert.Throws<InvalidOperationException>(() =>
            template.SetCurrentVersion(draft.Id));
    }

    [Fact]
    public void SettingCurrentVersion_LeavesAtMostOneCurrentVersion()
    {
        var creator = Guid.NewGuid();
        var template = CreateTemplate(creator);
        var first = template.Versions.Single();
        first.Publish(creator);
        template.SetCurrentVersion(first.Id);

        var second = template.AddVersion(
            "<p>Second</p>",
            ContentFormat.Html,
            creator);
        second.Publish(creator);
        template.SetCurrentVersion(second.Id);

        Assert.False(first.IsCurrent);
        Assert.True(second.IsCurrent);
        Assert.Single(template.Versions, version => version.IsCurrent);
    }

    [Fact]
    public void InactiveTemplate_CannotCreateDocument()
    {
        var creator = Guid.NewGuid();
        var template = CreateActiveTemplate(creator);
        template.Deactivate();

        Assert.Throws<InvalidOperationException>(() =>
            template.CreateDocument(creator, "New document"));
    }

    [Fact]
    public void FinalizedDocument_CannotBeEditedOrFinalizedAgain()
    {
        var creator = Guid.NewGuid();
        var template = CreateActiveTemplate(creator);
        var document = template.CreateDocument(creator, "Final document");
        document.Finalize();

        Assert.Throws<InvalidOperationException>(() =>
            document.UpdateDraft("Changed", "<p>Changed</p>"));
        Assert.Throws<InvalidOperationException>(() => document.Finalize());
    }

    [Fact]
    public void PlaceholderValue_RetainsHistoricalSnapshots()
    {
        var creator = Guid.NewGuid();
        var template = CreateTemplate(creator);
        var version = template.Versions.Single();
        var placeholder = version.AddPlaceholder(
            "client_email",
            "Client email",
            PlaceholderDataType.Email,
            true);
        version.Publish(creator);
        template.SetCurrentVersion(version.Id);
        template.Activate();
        var document = template.CreateDocument(creator, "Client record");

        var storedValue = document.SetPlaceholderValue(
            placeholder,
            "client@example.test");

        Assert.Equal(placeholder.Id, storedValue.PlaceholderId);
        Assert.Equal("client_email", storedValue.PlaceholderKeySnapshot);
        Assert.Equal("Client email", storedValue.LabelSnapshot);
        Assert.Equal(PlaceholderDataType.Email, storedValue.DataTypeSnapshot);
    }

    private static Template CreateTemplate(Guid? creator = null)
    {
        return new Template(
            "Agreement",
            Guid.NewGuid(),
            creator ?? Guid.NewGuid(),
            "<p>Agreement</p>",
            ContentFormat.Html);
    }

    private static Template CreateActiveTemplate(Guid creator)
    {
        var template = CreateTemplate(creator);
        var version = template.Versions.Single();
        version.Publish(creator);
        template.SetCurrentVersion(version.Id);
        template.Activate();
        return template;
    }
}
