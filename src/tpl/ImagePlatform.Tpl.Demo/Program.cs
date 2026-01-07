// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImagePlatform.Tpl.Processing;

static async Task Main()
{
    // 1) Pune 2-3 imagini în folderul: src/tpl/ImagePlatform.Tpl.Demo/input
    var inputDir = Path.Combine(AppContext.BaseDirectory, "input");
    var outputDir = Path.Combine(AppContext.BaseDirectory, "output");

    Directory.CreateDirectory(inputDir);
    Directory.CreateDirectory(outputDir);

    var files = Directory.GetFiles(inputDir)
        .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (files.Count == 0)
    {
        Console.WriteLine("Pune imagini in folderul: " + inputDir);
        return;
    }

    var images = files.Select(File.ReadAllBytes).ToList();

    var transforms = new List<ImageTransform>
    {
        new("resize", new Dictionary<string,string> { ["width"] = "800" }),
        new("grayscale"),
        new("compress", new Dictionary<string,string> { ["quality"] = "70" })
    };

    var pipeline = new ImageProcessingPipeline();

    Console.WriteLine($"Processing {images.Count} images with MaxDOP=4 ...");
    var results = await pipeline.ProcessBatchAsync(images, transforms, maxDegreeOfParallelism: 4);

    for (int i = 0; i < results.Count; i++)
    {
        var outPath = Path.Combine(outputDir, $"out_{i}.jpg");
        File.WriteAllBytes(outPath, results[i]);
        Console.WriteLine("Wrote: " + outPath);
    }

    Console.WriteLine("Done. Check output folder: " + outputDir);
}

await Main();
