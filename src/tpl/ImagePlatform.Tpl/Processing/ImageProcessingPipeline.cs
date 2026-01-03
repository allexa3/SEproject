using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImagePlatform.Tpl.Processing;

/// <summary>
/// Demonstrates TPL usage inside a worker: process many images concurrently.
/// Real image operations (resize/compress/filters) will be added later.
/// </summary>
public sealed class ImageProcessingPipeline
{
    public Task<byte[]> ProcessImageAsync(
        byte[] imageBytes,
        IReadOnlyList<ImageTransform> transforms,
        CancellationToken cancellationToken = default)
    {
        // For now we just "simulate" work. Replace with actual image processing later.
        // Sequential per image (operations usually depend on previous output).
        return Task.Run(async () =>
        {
            var current = imageBytes;
            foreach (var _ in transforms)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken); // simulate CPU work
                // TODO: apply actual transform and update 'current' with new bytes.
            }

            return current;
        }, cancellationToken);
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
            source: Enumerable.Range(0, images.Count),
            parallelOptions: options,
            body: async (i, ct) =>
            {
                results[i] = await ProcessImageAsync(images[i], transforms, ct);
            });

        return results;
    }
}


