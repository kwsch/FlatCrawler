using System.Collections.Generic;
using System.Linq;

namespace FlatCrawler.Lib;

public static class FieldAnalysis
{
    public static FieldAnalysisResult AnalyzeFields(this IArrayNode array, byte[] data)
        => AnalyzeFields(new FieldNodeSet(data, array.Entries.OfType<FlatBufferNodeField>().ToArray()));

    public static FieldAnalysisResult AnalyzeFields(this FlatBufferNodeField node, byte[] data)
        => AnalyzeFields(new FieldNodeSet(data, new[] {node}));

    public static FieldAnalysisResult AnalyzeFields(params FieldNodeSet[] input)
    {
        var result = new FieldAnalysisResult();

        // Scan all fields, determine size first.
        foreach (var set in input)
        {
            foreach (var entry in set.Entries)
                result.ScanFieldSize(entry);
        }

        result.GuessOverallType();

        // Scan all fields, determine type.
        foreach (var (data, entries) in input)
        {
            foreach (var entry in entries)
                result.ScanFieldType(entry, data);
        }

        return result;
    }
}

public sealed record FieldNodeSet(byte[] Data, IReadOnlyList<FlatBufferNodeField> Entries);
