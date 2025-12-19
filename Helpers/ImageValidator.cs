using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MaltalistApi.Helpers;

public static class ImageValidator
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

    // Magic bytes for common image formats
    private static readonly Dictionary<string, byte[][]> MagicBytes = new()
    {
        { "jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { "png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { "gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 } } },
        { "webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } } // RIFF header
    };

    /// <summary>
    /// Validates file extension, MIME type, and size
    /// </summary>
    public static void ValidateFileBasics(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("No file provided");

        // Validate file size
        if (file.Length > MaxFileSize)
            throw new InvalidOperationException($"File {file.FileName} exceeds maximum size of 5MB");

        // Validate file extension
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type {ext} is not allowed. Only JPG, PNG, GIF, and WEBP images are permitted.");

        // Validate MIME type (note: this is client-controlled and can be spoofed)
        if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            throw new InvalidOperationException($"Invalid MIME type {file.ContentType}. Only image files are permitted.");
    }

    /// <summary>
    /// Validates file content by checking magic bytes (file signature)
    /// </summary>
    public static async Task<bool> ValidateMagicBytesAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var buffer = new byte[8]; // Read first 8 bytes (enough for most formats)
        await stream.ReadAsync(buffer, 0, buffer.Length);

        // Check against known magic bytes
        foreach (var format in MagicBytes.Values)
        {
            foreach (var signature in format)
            {
                if (buffer.Take(signature.Length).SequenceEqual(signature))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates that the file is actually a valid image by attempting to load it
    /// This is the most reliable validation method
    /// </summary>
    public static async Task<bool> ValidateImageContentAsync(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            using var image = await Image.LoadAsync(stream);
            
            // If we can load it as an image, it's valid
            // Additional checks can be done here (e.g., dimensions, format)
            return image.Width > 0 && image.Height > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Comprehensive validation: extension, MIME type, magic bytes, and actual image content
    /// </summary>
    public static async Task ValidateImageFileAsync(IFormFile file)
    {
        // Step 1: Basic validation (size, extension, MIME type)
        ValidateFileBasics(file);

        // Step 2: Validate magic bytes
        if (!await ValidateMagicBytesAsync(file))
            throw new InvalidOperationException("File does not appear to be a valid image (invalid file signature)");

        // Step 3: Validate actual image content
        if (!await ValidateImageContentAsync(file))
            throw new InvalidOperationException("File is not a valid image or is corrupted");
    }

    /// <summary>
    /// Sanitizes and re-encodes an image file to remove potential malicious content
    /// Returns the sanitized image stream and recommended extension
    /// </summary>
    public static async Task<(MemoryStream stream, string extension)> SanitizeImageAsync(IFormFile file)
    {
        // Validate first
        await ValidateImageFileAsync(file);

        using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream);

        // Optional: Resize if too large (prevents DoS via huge images)
        const int maxDimension = 2048;
        if (image.Width > maxDimension || image.Height > maxDimension)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxDimension, maxDimension)
            }));
        }

        // Re-encode to remove any malicious content
        var outputStream = new MemoryStream();
        var originalExt = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Preserve original format when possible, default to JPEG for best compatibility
        switch (originalExt)
        {
            case ".png":
                await image.SaveAsPngAsync(outputStream);
                return (outputStream, ".png");
            case ".gif":
                await image.SaveAsGifAsync(outputStream);
                return (outputStream, ".gif");
            case ".webp":
                await image.SaveAsWebpAsync(outputStream);
                return (outputStream, ".webp");
            default: // .jpg, .jpeg, or unknown
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 90 });
                return (outputStream, ".jpg");
        }
    }
}
