using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Infrastructure.Persistence;
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
