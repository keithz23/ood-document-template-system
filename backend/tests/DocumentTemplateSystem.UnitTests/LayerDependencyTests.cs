using System.Reflection;

namespace DocumentTemplateSystem.UnitTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_DoesNotReferenceInfrastructureFrameworks()
    {
        var references = LoadReferences("DocumentTemplateSystem.Domain");

        Assert.DoesNotContain(references, name =>
            name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain("DocumentTemplateSystem.Api", references);
        Assert.DoesNotContain("DocumentTemplateSystem.Infrastructure", references);
    }

    [Fact]
    public void Application_ReferencesNeitherApiNorInfrastructure()
    {
        var references = LoadReferences("DocumentTemplateSystem.Application");

        Assert.DoesNotContain("DocumentTemplateSystem.Api", references);
        Assert.DoesNotContain("DocumentTemplateSystem.Infrastructure", references);
    }

    private static string[] LoadReferences(string assemblyName)
    {
        return Assembly
            .Load(assemblyName)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
    }
}

