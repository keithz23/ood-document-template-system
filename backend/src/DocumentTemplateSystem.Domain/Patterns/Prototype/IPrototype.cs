namespace DocumentTemplateSystem.Domain.Patterns.Prototype;

public interface IPrototype<out T>
{
    T Clone();
}
