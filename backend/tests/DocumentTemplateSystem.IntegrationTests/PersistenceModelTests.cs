using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Infrastructure.Persistence;
using DocumentTemplateSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DocumentTemplateSystem.IntegrationTests;

public sealed class PersistenceModelTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_tests;Username=postgres;Password=postgres")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Model_DefinesApprovedUniqueConstraints()
    {
        using var context = CreateContext();
        var model = context.Model;

        AssertUniqueIndex(model.FindEntityType(typeof(User)), nameof(User.Username));
        AssertUniqueIndex(model.FindEntityType(typeof(User)), nameof(User.Email));
        AssertUniqueIndex(
            model.FindEntityType(typeof(TemplateVersion)),
            nameof(TemplateVersion.TemplateId),
            nameof(TemplateVersion.VersionNumber));
        AssertUniqueIndex(
            model.FindEntityType(typeof(Placeholder)),
            nameof(Placeholder.TemplateVersionId),
            nameof(Placeholder.Key));

        var currentVersionIndex = model
            .FindEntityType(typeof(TemplateVersion))!
            .GetIndexes()
            .Single(index =>
                index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual([nameof(TemplateVersion.TemplateId)]));

        Assert.Equal("\"IsCurrent\" = TRUE", currentVersionIndex.GetFilter());
    }

    [Fact]
    public void Model_ProtectsHistoricalRelationshipsFromCascadeDelete()
    {
        using var context = CreateContext();
        var model = context.Model;

        Assert.Equal(
            DeleteBehavior.Restrict,
            FindForeignKey<Document, TemplateVersion>(model).DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.Restrict,
            FindForeignKey<TemplateVersion, Template>(model).DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.Restrict,
            FindForeignKey<DocumentPlaceholderValue, Document>(model).DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.SetNull,
            FindForeignKey<DocumentPlaceholderValue, Placeholder>(model).DeleteBehavior);
    }

    [Fact]
    public async Task AddTemplateVersion_MarksNewGraphAddedAndLeavesSourceUnchanged()
    {
        await using var context = CreateContext();
        var creatorId = Guid.NewGuid();
        var template = new Template(
            "Agreement",
            Guid.NewGuid(),
            creatorId,
            "<p>{{client_name}}</p>");
        var source = template.Versions.Single();
        source.AddPlaceholder(
            "client_name",
            "Client name",
            PlaceholderDataType.Text,
            true);
        source.Publish(creatorId);
        template.SetCurrentVersion(source.Id);
        context.Attach(template);

        var draft = template.AddVersion(
            source.Content,
            source.ContentFormat,
            creatorId);
        foreach (var placeholder in source.Placeholders)
        {
            draft.AddPlaceholder(
                placeholder.Key,
                placeholder.Label,
                placeholder.DataType,
                placeholder.IsRequired,
                placeholder.DefaultValue);
        }

        var repository = new AdminCatalogRepository(context);
        await repository.AddTemplateVersionAsync(draft);

        Assert.Equal(EntityState.Added, context.Entry(draft).State);
        Assert.All(
            draft.Placeholders,
            placeholder => Assert.Equal(
                EntityState.Added,
                context.Entry(placeholder).State));
        Assert.Equal(EntityState.Unchanged, context.Entry(source).State);
        Assert.All(
            source.Placeholders,
            placeholder => Assert.Equal(
                EntityState.Unchanged,
                context.Entry(placeholder).State));
    }

    private static void AssertUniqueIndex(
        IReadOnlyEntityType? entityType,
        params string[] propertyNames)
    {
        Assert.NotNull(entityType);
        Assert.Contains(entityType.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual(propertyNames));
    }

    private static IReadOnlyForeignKey FindForeignKey<TDependent, TPrincipal>(
        IModel model)
    {
        return model.FindEntityType(typeof(TDependent))!
            .GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(TPrincipal));
    }
}
