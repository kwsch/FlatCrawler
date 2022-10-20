using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlatCrawler.Lib;

/// <summary>
/// Logic to iterate through multiple FlatBuffer files and log their potential schemas.
/// </summary>
public static class FileAnalysis
{
    /// <summary>
    /// Iterates through all files in the specified directory and analyzes the specified nodes.
    /// </summary>
    /// <param name="path">Directory to search for files.</param>
    /// <param name="dest">
    /// Destination directory to save the results.
    /// If none specified, the results will be saved in the same directory as the executable.
    /// </param>
    public static void IterateAndDump(string path, string dest = "")
    {
        if (string.IsNullOrEmpty(dest))
        {
            // Set the destination to the executable's directory, with a subfolder named FlatAnalysis.
            var entry = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var dir = Path.GetDirectoryName(entry);
            dest = Path.Combine(dir ?? string.Empty, "FlatAnalysis");
        }

        var settings = new FileAnalysisSettings(path, dest);
        IterateAndDump(settings);
    }

    /// <inheritdoc cref="IterateAndDump(string,string)"></inheritdoc>
    public static void IterateAndDump(FileAnalysisSettings settings)
    {
        // Ensure the destination directory exists for the output.
        var dest = settings.OutputPath;
        if (!Directory.Exists(dest))
            Directory.CreateDirectory(dest);

        var files = Directory.EnumerateFiles(settings.InputPath, settings.SearchPattern, SearchOption.AllDirectories);

        Span<byte> buffer = new byte[settings.MaxPeekSize].AsSpan();
        List<FileAnalysisResult> results = new();

        foreach (var file in files)
        {
            if (settings.SkipAnalysisIfSchemaDumpExists)
            {
                var outPath = settings.GetOutputPath(file);
                if (File.Exists(outPath))
                    continue;
            }

            try
            {
                bool result = TryAnalyzeFile(settings, file, results, buffer);
                if (result)
                    Console.WriteLine(results[^1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing {file}: {ex.Message}");
            }
        }

        // Dump the results to a file.
        var outputResultsPath = Path.Combine(settings.OutputPath, settings.AllResultOutputFileName);
        ExportMetadata(results, outputResultsPath);
    }

    private static void ExportMetadata(IEnumerable<FileAnalysisResult> results, string filePath)
    {
        using var swResults = File.CreateText(filePath);
        var ordered = results.GroupBy(z => z.FieldCount).OrderBy(z => z.Key);
        foreach (var fc in ordered)
        {
            swResults.WriteLine($"Field count: {fc.Key}");
            var entries = fc.GroupBy(z => z.Hash);
            foreach (var entry in entries)
            {
                swResults.WriteLine($"\tHash: {entry.Key}");
                var reordered = entry
                    .OrderBy(z => z.FileName)
                    .ThenBy(z => z.Path);
                foreach (var result in reordered)
                    swResults.WriteLine($"\t\t{result}");
            }
            swResults.WriteLine();
        }
    }

    private static bool TryAnalyzeFile(FileAnalysisSettings settings, string file, ICollection<FileAnalysisResult> results, Span<byte> buffer)
    {
        using var fs = File.OpenRead(file);
        if (fs.Length > buffer.Length)
            return false;

        var length = Math.Min(buffer.Length, fs.Length);
        var data = buffer[..(int)length];
        var read = fs.Read(data);

        fs.Dispose();
        if (read != length)
            throw new Exception("Read less than expected.");

        return ReadAndDump(settings, file, results, data);
    }

    private static bool ReadAndDump(FileAnalysisSettings settings, string file, ICollection<FileAnalysisResult> results, ReadOnlySpan<byte> data)
    {
        // Do something with the buffer.
        if (!FlatBufferFile.GetIsSizeValid(data))
            return false;

        // Read a new root node from the provided data
        var root = FlatBufferRoot.Read(0, data);

        // Get an analysis of the root node.
        var analysis = root.AnalyzeFields(data);

        // Dump the analysis to a file.
        var info = DumpFile(settings, file, analysis, root, data);
        results.Add(info);
        return true;
    }

    /// <summary>
    /// Dumps the analysis of a FlatBuffer file to a text file for later viewing.
    /// </summary>
    /// <param name="settings">The settings to use for the analysis.</param>
    /// <param name="file">The path to the file that was analyzed.</param>
    /// <param name="analysis">The analysis of the file.</param>
    /// <param name="node">The node that served as the root for analysis.</param>
    /// <param name="data">The data that was analyzed.</param>
    /// <returns>Summary object of the analysis.</returns>
    private static FileAnalysisResult DumpFile(FileAnalysisSettings settings, string file, FieldAnalysisResult analysis, FlatBufferNodeField node, ReadOnlySpan<byte> data)
    {
        // Create a unique hash based on the field data.
        if (!settings.DumpIndividualSchemaAnalysis)
            return DumpSingleResult(file, analysis, node);

        var outputPath = settings.GetOutputPath(file);
        using var sw = File.CreateText(outputPath);

        int hash = 0;
        RecursiveDump(node, data, analysis.Fields, sw, ref hash, settings);
        // Return the file analysis result.
        return new(node.FieldCount, hash, Path.GetFileName(file), file);
    }

    public static void RecursiveDump(FlatBufferNodeField node, ReadOnlySpan<byte> data, Dictionary<int, FieldObservations> fields, TextWriter sw, ref int hash, ISchemaAnalysisSettings settings, int depth = 0)
    {
        var ordered = fields.OrderBy(z => z.Key);
        foreach (var (index, obs) in ordered)
        {
            var line = $"[{index}] {obs.Summary(node, index, data)}";
            var padded = line.PadLeft(depth + line.Length, '\t');
            sw.WriteLine(padded);
            hash = HashCode.Combine(hash, obs.GetHashCode());

            if (depth > settings.MaxRecursionDepth)
                continue;

            // Peek deeper if possible.
            if (obs.Type.IsPotentialObject)
            {
                var child = node.ReadAsObject(data, index);
                var analysis = child.AnalyzeFields(data);
                if (analysis.IsAnyFieldRecognized)
                {
                    var header = $"{"".PadLeft(depth + 1, '\t')}As Object:";
                    sw.WriteLine(header);
                    RecursiveDump(child, data, analysis.Fields, sw, ref hash, settings, depth + 1);
                }
            }
            if (obs.Type.IsPotentialObjectArray)
            {
                var child = node.ReadAsTable(data, index);
                var analysis = child.AnalyzeFields(data);
                if (analysis.IsAnyFieldRecognized)
                {
                    var header = $"{"".PadLeft(depth + 1, '\t')}As Object[]:";
                    sw.WriteLine(header);
                    RecursiveDump(child.GetEntry(0), data, analysis.Fields, sw, ref hash, settings, depth + 1);
                }
            }
        }
    }

    private static FileAnalysisResult DumpSingleResult(string file, FieldAnalysisResult analysis, FlatBufferNodeField node)
    {
        var hash = 0;
        var ordered = analysis.Fields.OrderBy(z => z.Key);
        foreach (var (_, obs) in ordered)
            hash = HashCode.Combine(hash, obs.GetHashCode());
        // Return the file analysis result.
        return new(node.FieldCount, hash, Path.GetFileName(file), file);
    }
}
