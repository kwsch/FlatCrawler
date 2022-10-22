using System;
using System.Collections.Generic;
using System.Linq;

namespace FlatCrawler.Lib;

/// <summary>
/// Analyzes nodes with the same schema to determine the possible types of each field.
/// </summary>
public static class FieldAnalysis
{
    public static FieldAnalysisResult AnalyzeFields(this IArrayNode array, ReadOnlySpan<byte> data)
        => AnalyzeFields(data, array.Entries.OfType<FlatBufferNodeField>().ToArray());

    public static FieldAnalysisResult AnalyzeFields(this FlatBufferNodeField node, ReadOnlySpan<byte> data)
        => AnalyzeFields(data, node);

    public static FieldAnalysisResult AnalyzeFields(ReadOnlySpan<byte> data, params FlatBufferNodeField[] nodes)
    {
        var result = new FieldAnalysisResult();
        result.ScanFieldSize(nodes);
        result.GuessOverallType();
        result.ScanFieldType(nodes, data);
        return result;
    }

    /// <summary>
    /// Useful for analyzing the fields from multiple FlatBuffer input files that share the same schema.
    /// </summary>
    /// <param name="paths">File paths to analyze.</param>
    /// <param name="fieldSelector">Navigation method to get the node to start analyzing.</param>
    public static FieldAnalysisResult AnalyzeFields(IEnumerable<string> paths, Func<FlatBufferRoot, FlatBufferFile, IEnumerable<FlatBufferNodeField>> fieldSelector)
    {
        var sources = paths.Select(x => new FlatBufferFile(x));
        return AnalyzeFields(sources, fieldSelector);
    }

    public static FieldAnalysisResult AnalyzeFields(IEnumerable<FlatBufferFile> sources, Func<FlatBufferRoot, FlatBufferFile, IEnumerable<FlatBufferNodeField>> fieldSelector)
    {
        var result = new FieldAnalysisResult();
        var temp = new List<NodeStorage<FlatBufferNodeField>>();
        foreach (var file in sources)
        {
            var root = FlatBufferRoot.Read(file, 0);
            var fields = fieldSelector(root, file).ToArray();

            result.ScanFieldSize(fields);

            // Retain for later use
            temp.Add(new(file, fields));
        }

        result.GuessOverallType();

        foreach (var x in temp)
            result.ScanFieldType(x.Nodes, x.File.Data);
        return result;
    }

    private readonly record struct NodeStorage<T>(FlatBufferFile File, IReadOnlyList<T> Nodes);
}
