using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

using SixLabors.ImageSharp.Formats.Png;


namespace ImagePlatform.Tpl.Processing;

/// <summary>
/// TPL pipeline: processes images concurrently (bounded parallelism),
/// while applying transforms sequentially per image.
/// </summary>
public sealed class ImageProcessingPipeline
{
    public Task<byte[]> ProcessImageAsync(
        byte[] imageBytes,
        IReadOnlyList<ImageTransform> transforms,
        CancellationToken cancellationToken = default)
    {
        if (imageBytes is null) throw new ArgumentNullException(nameof(imageBytes));
        if (transforms is null) throw new ArgumentNullException(nameof(transforms));

        cancellationToken.ThrowIfCancellationRequested();

        using var inputStream = new MemoryStream(imageBytes, writable: false);
        using Image image = Image.Load(inputStream);


        int? jpegQualityOverride = null;

        foreach (var transform in transforms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyTransform(image, transform, ref jpegQualityOverride);
        }

        using var outputStream = new MemoryStream();

        if (jpegQualityOverride.HasValue)
        {
            var encoder = new JpegEncoder { Quality = jpegQualityOverride.Value };
            image.Save(outputStream, encoder);
        }
        else
        {
            image.SaveAsPng(outputStream);
        }

        return Task.FromResult(outputStream.ToArray());
    }

    public async Task<IReadOnlyList<byte[]>> ProcessBatchAsync(
        IReadOnlyList<byte[]> images,
        IReadOnlyList<ImageTransform> transforms,
        int maxDegreeOfParallelism = 0,
        CancellationToken cancellationToken = default)
    {
        if (images is null) throw new ArgumentNullException(nameof(images));
        if (transforms is null) throw new ArgumentNullException(nameof(transforms));

        var results = new byte[images.Count][];

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = maxDegreeOfParallelism <= 0
                ? Environment.ProcessorCount
                : maxDegreeOfParallelism
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(0, images.Count),
            options,
            async (i, ct) =>
            {
                results[i] = await ProcessImageAsync(images[i], transforms, ct);
            });

        return results;
    }

    private static void ApplyTransform(
        Image image,
        ImageTransform transform,
        ref int? jpegQualityOverride)
    {
        var name = transform.Name.ToLowerInvariant();
        var p = transform.Parameters;

        switch (name)
        {
            case "resize":
                int? w = TryGetInt(p, "width");
                int? h = TryGetInt(p, "height");

                if (w is null && h is null)
                    throw new ArgumentException("Resize requires width or height.");

                image.Mutate(ctx =>
                {
                    ctx.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(w ?? 0, h ?? 0)
                    });
                });
                break;

            case "grayscale":
                image.Mutate(ctx => ctx.Grayscale());
                break;

            case "compress":
                int q = TryGetInt(p, "quality") ?? 75;
                jpegQualityOverride = Math.Clamp(q, 1, 100);
                break;

            default:
                throw new NotSupportedException($"Unknown transform '{transform.Name}'.");
        }
    }

    private static int? TryGetInt(
        IReadOnlyDictionary<string, string>? p,
        string key)
    {
        if (p == null) return null;
        if (!p.TryGetValue(key, out var value)) return null;
        return int.TryParse(value, out var n) ? n : null;
    }
}
