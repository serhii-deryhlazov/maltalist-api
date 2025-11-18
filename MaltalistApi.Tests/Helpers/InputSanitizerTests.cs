using Xunit;
using MaltalistApi.Helpers;

namespace MaltalistApi.Tests.Helpers;

public class InputSanitizerTests
{
    [Fact]
    public void SanitizeHtml_EncodesHtmlTags()
    {
        // Arrange
        var input = "<script>alert('xss')</script>";

        // Act
        var result = InputSanitizer.SanitizeHtml(input);

        // Assert
        Assert.DoesNotContain("<script>", result);
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public void SanitizeHtml_EncodesSpecialCharacters()
    {
        // Arrange
        var input = "Test & <b>Bold</b>";

        // Act
        var result = InputSanitizer.SanitizeHtml(input);

        // Assert
        Assert.Contains("&amp;", result);
        Assert.Contains("&lt;b&gt;", result);
        Assert.Contains("&lt;/b&gt;", result);
    }

    [Fact]
    public void SanitizeHtml_ReturnsEmptyString_WhenInputIsNull()
    {
        // Act
        var result = InputSanitizer.SanitizeHtml(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeHtml_ReturnsEmptyString_WhenInputIsWhitespace()
    {
        // Act
        var result = InputSanitizer.SanitizeHtml("   ");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeText_RemovesControlCharacters()
    {
        // Arrange - Test control character removal by building string with actual control chars
        var inputWithControlChars = "Test" + (char)0x01 + (char)0x02 + "Text";

        // Act
        var result = InputSanitizer.SanitizeText(inputWithControlChars);

        // Assert - Result should not contain control characters
        Assert.Equal("TestText", result);
        Assert.False(result.Contains((char)0x01));
        Assert.False(result.Contains((char)0x02));
    }

    [Fact]
    public void SanitizeText_NormalizesWhitespace()
    {
        // Arrange
        var input = "Test    multiple   spaces";

        // Act
        var result = InputSanitizer.SanitizeText(input);

        // Assert
        Assert.Equal("Test multiple spaces", result);
    }

    [Fact]
    public void SanitizeText_TrimsWhitespace()
    {
        // Arrange
        var input = "  Test  ";

        // Act
        var result = InputSanitizer.SanitizeText(input);

        // Assert
        Assert.Equal("Test", result);
    }

    [Fact]
    public void SanitizeText_ReturnsEmptyString_WhenInputIsNull()
    {
        // Act
        var result = InputSanitizer.SanitizeText(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeUrl_AcceptsValidHttpUrl()
    {
        // Arrange
        var input = "http://example.com/image.jpg";

        // Act
        var result = InputSanitizer.SanitizeUrl(input);

        // Assert
        Assert.Equal("http://example.com/image.jpg", result);
    }

    [Fact]
    public void SanitizeUrl_AcceptsValidHttpsUrl()
    {
        // Arrange
        var input = "https://example.com/image.jpg";

        // Act
        var result = InputSanitizer.SanitizeUrl(input);

        // Assert
        Assert.Equal("https://example.com/image.jpg", result);
    }

    [Fact]
    public void SanitizeUrl_RejectsJavascriptProtocol()
    {
        // Arrange
        var input = "javascript:alert('xss')";

        // Act
        var result = InputSanitizer.SanitizeUrl(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeUrl_RejectsDataProtocol()
    {
        // Arrange
        var input = "data:text/html,<script>alert('xss')</script>";

        // Act
        var result = InputSanitizer.SanitizeUrl(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeUrl_RejectsPathTraversal()
    {
        // Arrange
        var input = "../../../etc/passwd";

        // Act
        var result = InputSanitizer.SanitizeUrl(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeUrl_RejectsDoubleSlashWithoutProtocol()
    {
        // Arrange
        var input = "//evil.com/image.jpg";

        // Act
        var result = InputSanitizer.SanitizeUrl(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeUrl_RejectsBackslashes()
    {
        // Arrange
        var input = "path\\to\\file.jpg";

        // Act
        var result = InputSanitizer.SanitizeUrl(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeUrl_AcceptsRelativePath()
    {
        // Arrange
        var input = "images/user/profile.jpg";

        // Act
        var result = InputSanitizer.SanitizeUrl(input);

        // Assert
        Assert.Equal("images/user/profile.jpg", result);
    }

    [Fact]
    public void SanitizeUrl_ReturnsNull_WhenInputIsNull()
    {
        // Act
        var result = InputSanitizer.SanitizeUrl(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeEmail_AcceptsValidEmail()
    {
        // Arrange
        var input = "test@example.com";

        // Act
        var result = InputSanitizer.SanitizeEmail(input);

        // Assert
        Assert.Equal("test@example.com", result);
    }

    [Fact]
    public void SanitizeEmail_ConvertsToLowercase()
    {
        // Arrange
        var input = "Test@Example.COM";

        // Act
        var result = InputSanitizer.SanitizeEmail(input);

        // Assert
        Assert.Equal("test@example.com", result);
    }

    [Fact]
    public void SanitizeEmail_RejectsInvalidFormat_NoAtSign()
    {
        // Arrange
        var input = "invalidemail.com";

        // Act
        var result = InputSanitizer.SanitizeEmail(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeEmail_RejectsInvalidFormat_NoDomain()
    {
        // Arrange
        var input = "test@";

        // Act
        var result = InputSanitizer.SanitizeEmail(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeEmail_RejectsInvalidFormat_NoUsername()
    {
        // Arrange
        var input = "@example.com";

        // Act
        var result = InputSanitizer.SanitizeEmail(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeEmail_RejectsInvalidFormat_MultipleAtSigns()
    {
        // Arrange
        var input = "test@@example.com";

        // Act
        var result = InputSanitizer.SanitizeEmail(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeEmail_TrimsWhitespace()
    {
        // Arrange
        var input = "  test@example.com  ";

        // Act
        var result = InputSanitizer.SanitizeEmail(input);

        // Assert
        Assert.Equal("test@example.com", result);
    }

    [Fact]
    public void SanitizeEmail_ReturnsNull_WhenInputIsNull()
    {
        // Act
        var result = InputSanitizer.SanitizeEmail(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeEmail_ReturnsNull_WhenInputIsWhitespace()
    {
        // Act
        var result = InputSanitizer.SanitizeEmail("   ");

        // Assert
        Assert.Null(result);
    }
}
