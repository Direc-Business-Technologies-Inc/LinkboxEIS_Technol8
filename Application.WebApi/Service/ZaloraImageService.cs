using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System.Text.RegularExpressions;

namespace Application.WebApi.Service
{
    public class ZaloraImageService
    {
        public async Task<string> PostImage(string base64, string filename, string fileid, string sku)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("base64 is required", nameof(base64));

            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("filename is required", nameof(filename));

            if (string.IsNullOrWhiteSpace(fileid))
                throw new ArgumentException("fileid is required", nameof(fileid));

            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("sku is required", nameof(sku));

            // ✅ sanitize sku + filename (prevent traversal + invalid chars)
            sku = SanitizeSegment(sku);
            filename = Path.GetFileName(filename);
            foreach (var c in Path.GetInvalidFileNameChars())
                filename = filename.Replace(c, '_');

            // ✅ handle data URI (data:image/jpeg;base64,...)
            string? dataUriExt = null;
            var m = Regex.Match(base64, @"^data:image/(?<ext>[a-zA-Z0-9+]+);base64,(?<data>.+)$");
            if (m.Success)
            {
                dataUriExt = m.Groups["ext"].Value.ToLowerInvariant();
                base64 = m.Groups["data"].Value;
            }

            // ✅ decide extension
            var ext = dataUriExt ?? Path.GetExtension(filename).TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext))
                ext = "jpg";

            if (!Path.HasExtension(filename))
                filename = $"{filename}.{ext}";

            // ✅ Images/<sku>/
            var imagesRoot = Path.Combine(AppContext.BaseDirectory, "Images");
            var skuFolder = Path.Combine(imagesRoot, sku);
            Directory.CreateDirectory(skuFolder);

            var savedFileName = $"{fileid}_{filename}";
            var savePath = Path.Combine(skuFolder, savedFileName);

            // ✅ skip if exists
            if (File.Exists(savePath))
                return savedFileName;

            // ✅ decode base64 -> bytes
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid base64.", nameof(base64), ex);
            }

            // ✅ load + resize + save
            await using var input = new MemoryStream(bytes);
            using var image = await Image.LoadAsync(input);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(762, 1100),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            await image.SaveAsync(savePath);

            return savedFileName;
        }

        private static string SanitizeSegment(string value)
        {
            value = value.Trim();
            value = Path.GetFileName(value); // removes any path segments

            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            value = value.Replace(Path.DirectorySeparatorChar, '_')
                         .Replace(Path.AltDirectorySeparatorChar, '_');

            return value;
        }
        public IActionResult GetImageFromFolder(string sku, string fileName)
        {
            sku = Path.GetFileName(sku);
            fileName = Path.GetFileName(fileName);

            var imagesRoot = Path.Combine(AppContext.BaseDirectory, "Images");
            var fullPath = Path.Combine(imagesRoot, sku, fileName);

            if (!System.IO.File.Exists(fullPath))
                return new NotFoundObjectResult(new { message = $"File not found: {sku}/{fileName}" });

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fullPath, out var contentType))
                contentType = "application/octet-stream";

            return new PhysicalFileResult(fullPath, contentType);
        }
        //public (string FullPath, string ContentType)? TryResolveImage(string fileName)
        //{
        //    fileName = Path.GetFileName(fileName); // prevent path traversal

        //    var imagesPath = Path.Combine(AppContext.BaseDirectory, "Images");
        //    var fullPath = Path.Combine(imagesPath, fileName);

        //    if (!File.Exists(fullPath))
        //        return null;

        //    var provider = new FileExtensionContentTypeProvider();
        //    if (!provider.TryGetContentType(fullPath, out var contentType))
        //        contentType = "application/octet-stream";

        //    return (fullPath, contentType);
        //}
    }
}
