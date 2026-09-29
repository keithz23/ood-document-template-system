using DocumentTemplateSystem.Domain.Patterns.Composite;

namespace DocumentTemplateSystem.UnitTests;

public sealed class CompositePatternTests
{
    [Fact]
    public void LeafComponents_RenderHtmlSafely()
    {
        var text = new TextComponent("Terms < conditions");
        var image = new ImageComponent("/logo?a=1&b=2", "Company \"logo\"");

        Assert.Equal("Terms &lt; conditions", text.Render());
        Assert.Equal(
            "<img src=\"/logo?a=1&amp;b=2\" alt=\"Company &quot;logo&quot;\" />",
            image.Render());
    }

    [Fact]
    public void Section_RendersNestedChildrenRecursively()
    {
        var nested = new SectionComponent([
            new TextComponent("Nested")
        ]);
        var root = new SectionComponent([
            new TextComponent("Start"),
            nested,
            new TextComponent("End")
        ]);

        var rendered = root.Render();

        Assert.Equal(
            "<section>Start<section>Nested</section>End</section>",
            rendered);
    }

    [Fact]
    public void SectionClone_DeepCopiesChildren()
    {
        var originalText = new TextComponent("Original");
        var originalNested = new SectionComponent([originalText]);
        var original = new SectionComponent([originalNested]);

        var clone = Assert.IsType<SectionComponent>(original.Clone());
        var clonedNested = Assert.IsType<SectionComponent>(clone.Children.Single());
        var clonedText = Assert.IsType<TextComponent>(clonedNested.Children.Single());

        originalText.UpdateText("Changed");

        Assert.NotSame(originalNested, clonedNested);
        Assert.NotSame(originalText, clonedText);
        Assert.Equal("<section><section>Changed</section></section>", original.Render());
        Assert.Equal("<section><section>Original</section></section>", clone.Render());
    }
}
