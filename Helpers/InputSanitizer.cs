using System.Net;
using System.Text.RegularExpressions;

namespace MaltalistApi.Helpers;

public static class InputSanitizer
{
    /// <summary>
    /// Sanitizes HTML input to prevent XSS attacks
    /// </summary>
    public static string SanitizeHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // HTML encode the input
        var sanitized = WebUtility.HtmlEncode(input);
        
        return sanitized;
    }

    /// <summary>
    /// Sanitizes text input by removing potentially dangerous characters
    /// </summary>
    public static string SanitizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove control characters except newline, carriage return, and tab
        var sanitized = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");
        
        // Trim excessive whitespace
        sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();
        
        return sanitized;
    }

    /// <summary>
    /// Validates and sanitizes URL input
    /// </summary>
    public static string? SanitizeUrl(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim();

        // Check if it's a full URL
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            // Only allow http and https schemes
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                return uri.ToString();
            }
            return null;
        }

        // Allow relative paths and filenames (for images stored locally)
        // Remove any potentially dangerous characters
        if (input.Contains("..") || input.Contains("://") || input.Contains("\\"))
            return null;

        return input;
    }

    /// <summary>
    /// Sanitizes email input
    /// </summary>
    public static string? SanitizeEmail(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim().ToLowerInvariant();
        
        // Basic email validation
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        if (emailRegex.IsMatch(input))
        {
            return input;
        }

        return null;
    }
}
