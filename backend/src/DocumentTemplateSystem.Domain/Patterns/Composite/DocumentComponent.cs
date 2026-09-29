namespace DocumentTemplateSystem.Domain.Patterns.Composite;

public abstract class DocumentComponent
{
    public abstract string Render();

    public abstract DocumentComponent Clone();
}
