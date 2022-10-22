using System.IO;

namespace FlatCrawler.Lib;

/// <summary>
/// Settings to use when parsing multiple FlatBuffer files.
/// </summary>
/// <param name="InputPath">Root folder to search within.</param>
/// <param name="OutputPath">Folder to output the analysis results to.</param>
public sealed record FileAnalysisSettings(string InputPath, string OutputPath) : ISchemaAnalysisSettings
{
    /// <summary>
    /// File name search pattern.
    /// </summary>
    public string SearchPattern { get; init; } = "*.*";

    /// <summary>
    /// The maximum number of bytes to peek at when analyzing a file.
    /// Any file larger than this will be skipped.
    /// </summary>
    public int MaxPeekSize { get; init; } = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Relative file name of the output file that contains all result record lines.
    /// </summary>
    public string AllResultOutputFileName { get; init; } = "AllFlatBufferMetadata_{0}.txt";

    /// <summary>
    /// Export a schema dump file for each file analyzed.
    /// </summary>
    public bool DumpIndividualSchemaAnalysis { get; init; } = true;

    /// <summary>
    /// Set to true to skip analyzing a file if a previous schema dump file already exists.
    /// </summary>
    public bool SkipAnalysisIfSchemaDumpExists { get; init; }

    /// <summary>
    /// Output file name format for individual schema dumps.
    /// </summary>
    public string SchemaDumpFormat { get; init; } = "{0}.txt";

    /// <summary>
    /// Max recursion depth in object tree for analyzing child schemas.
    /// </summary>
    public int MaxRecursionDepth { get; init; } = 5;

    /// <summary>
    /// Gets the full path to the output file that contains individual schema analysis lines.
    /// </summary>
    /// <param name="fileName">Full source file name</param>
    /// <returns>Full destination file name</returns>
    public string GetOutputPath(string fileName)
    {
        var file = string.Format(SchemaDumpFormat, Path.GetFileName(fileName));
        return Path.Combine(OutputPath, file);
    }

    /// <summary>
    /// Gets the full path to the output file that contains all schema analysis lines.
    /// </summary>
    /// <param name="extension">File Extension</param>
    /// <returns>Full destination file name</returns>
    public string GetOutputPathMetadata(string extension)
    {
        var file = string.Format(AllResultOutputFileName, extension);
        return Path.Combine(OutputPath, file);
    }
}
