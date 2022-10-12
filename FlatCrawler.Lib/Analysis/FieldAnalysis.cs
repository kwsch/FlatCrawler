using System;
using System.Collections.Generic;
using System.IO;
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

    public static FieldAnalysisResult AnalyzeFields(IEnumerable<string> paths, Func<FlatBufferRoot, byte[], IEnumerable<FlatBufferNodeField>> fieldSelector)
    {
        var sources = paths.Select(File.ReadAllBytes);
        return AnalyzeFields(sources, fieldSelector);
    }

    public static FieldAnalysisResult AnalyzeFields(IEnumerable<byte[]> sources, Func<FlatBufferRoot, byte[], IEnumerable<FlatBufferNodeField>> fieldSelector)
    {
        var result = new FieldAnalysisResult();
        var temp = new List<NodeStorage<FlatBufferNodeField>>();
        foreach (var data in sources)
        {
            var root = FlatBufferRoot.Read(0, data);
            var fields = fieldSelector(root, data).ToArray();

            result.ScanFieldSize(fields);

            // Retain for later use
            temp.Add(new(data, fields));
        }

        result.GuessOverallType();

        foreach (var x in temp)
            result.ScanFieldType(x.Nodes, x.Data);
        return result;
    }

    private readonly record struct NodeStorage<T>(byte[] Data, IReadOnlyList<T> Nodes);
}
