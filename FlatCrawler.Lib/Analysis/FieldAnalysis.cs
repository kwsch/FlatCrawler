using System.Linq;

namespace FlatCrawler.Lib;

public static class FieldAnalysis
{
    public static FieldAnalysisResult AnalyzeFields(this IArrayNode array, byte[] data)
        => AnalyzeFields(data, array.Entries.OfType<FlatBufferNodeField>().ToArray());

    public static FieldAnalysisResult AnalyzeFields(this FlatBufferNodeField node, byte[] data)
        => AnalyzeFields(data, node);

    public static FieldAnalysisResult AnalyzeFields(byte[] data, params FlatBufferNodeField[] entries)
    {
        var result = new FieldAnalysisResult();

        // Scan all fields, determine size first.
        foreach (var entry in entries)
            result.ScanFieldSize(entry);

        result.GuessOverallType();

        // Scan all fields, determine type.
        foreach (var entry in entries)
            result.ScanFieldType(entry, data);

        return result;
    }
}
