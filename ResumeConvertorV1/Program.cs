using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ResumeConvertorV1;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("==== Resume Batch Conversion Started ====");

        // Define the folder to scan for resumes
        string resumeFolder = @"D:\Source\Work Projects\ResumeConvertorV1\ResumeConvertorV1\resumes";

        if (!Directory.Exists(resumeFolder))
        {
            Console.WriteLine($"Folder not found: {resumeFolder}");
            return;
        }

        string[] resumeFiles = Directory.GetFiles(resumeFolder, "*.*", SearchOption.TopDirectoryOnly)
                                        ?? Array.Empty<string>();

        // Filter only .pdf or .txt
        var validFiles = Array.FindAll(resumeFiles, f =>
            f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

        if (validFiles.Length == 0)
        {
            Console.WriteLine("No .pdf or .txt files found in folder.");
            return;
        }

        var totalStopwatch = Stopwatch.StartNew();
        var converter = new ResumeConverter(debug: true);

        foreach (string file in validFiles)
        {
            Console.WriteLine($"\nConverting: {Path.GetFileName(file)}");

            var fileStopwatch = Stopwatch.StartNew();
            try
            {
                await converter.RunAsync(file);
                Console.WriteLine($"Done in {fileStopwatch.Elapsed.TotalSeconds:F2} sec");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        totalStopwatch.Stop();
        Console.WriteLine($"\nBatch completed in {totalStopwatch.Elapsed.TotalSeconds:F2} sec");
        Console.WriteLine("==== Resume Batch Conversion Finished ====");
    }
}
