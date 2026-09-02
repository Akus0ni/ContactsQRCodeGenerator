using ContactQR.Core.VCard;
using FluentAssertions;

namespace ContactQR.Core.Tests.VCard;

public sealed class VCardTextEscaperTests
{
    [Fact]
    public void Escape_LeavesOrdinaryTextUntouched()
    {
        VCardTextEscaper.Escape("Sunrise Physiotherapy").Should().Be("Sunrise Physiotherapy");
    }

    [Fact]
    public void Escape_EscapesComma_BecauseUnescapedCommasSplitPropertiesOnTheScanningPhone()
    {
        VCardTextEscaper.Escape("Acme Interiors Pvt Ltd, Mumbai")
            .Should().Be("Acme Interiors Pvt Ltd\\, Mumbai");
    }

    [Fact]
    public void Escape_EscapesSemicolon()
    {
        VCardTextEscaper.Escape("Design; Print").Should().Be("Design\\; Print");
    }

    [Fact]
    public void Escape_EscapesBackslash()
    {
        VCardTextEscaper.Escape(@"A\B").Should().Be(@"A\\B");
    }

    [Fact]
    public void Escape_EscapesBackslashBeforeOtherCharacters_SoIntroducedBackslashesAreNotDoubled()
    {
        VCardTextEscaper.Escape(@"A\,B").Should().Be(@"A\\\,B");
    }

    [Fact]
    public void Escape_CollapsesCarriageReturnLineFeedToOneEscapedNewline()
    {
        VCardTextEscaper.Escape("Line one\r\nLine two").Should().Be("Line one\\nLine two");
    }

    [Fact]
    public void Escape_EscapesBareLineFeed()
    {
        VCardTextEscaper.Escape("Line one\nLine two").Should().Be("Line one\\nLine two");
    }

    [Fact]
    public void Escape_EscapesBareCarriageReturn()
    {
        VCardTextEscaper.Escape("Line one\rLine two").Should().Be("Line one\\nLine two");
    }

    [Fact]
    public void Escape_ReturnsEmpty_ForEmptyInput()
    {
        VCardTextEscaper.Escape(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Escape_Throws_WhenValueIsNull()
    {
        var escape = () => VCardTextEscaper.Escape(null!);

        escape.Should().Throw<ArgumentNullException>();
    }
}
