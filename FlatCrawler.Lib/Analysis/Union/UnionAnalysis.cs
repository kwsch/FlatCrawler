using System;
using System.Collections.Generic;

namespace FlatCrawler.Lib;

/// <summary>
/// Analyzes union-type nodes with the same schema to determine the possible types of each union.
/// </summary>
public static class UnionAnalysis
{
    public static UnionAnalysisResult AnalyzeUnion(this IArrayNode array, ReadOnlySpan<byte> data)
    {
        var result = GetUnionAnalysis(array, data);
        return new UnionAnalysisResult(result);
    }

    private static Dictionary<byte, UnionNodeSummary> GetUnionAnalysis(IArrayNode array, ReadOnlySpan<byte> data)
        => FlatBufferUnionNodeSummaries(array.Entries, data);

    private static Dictionary<byte, UnionNodeSummary> FlatBufferUnionNodeSummaries(IReadOnlyList<FlatBufferNode> entries, ReadOnlySpan<byte> data)
    {
        var result = new Dictionary<byte, UnionNodeSummary>();
        for (var index = 0; index < entries.Count; index++)
        {
            var flatBufferNode = entries[index];
            var node = (FlatBufferObject)flatBufferNode;
            var type = node.ReadAs<byte>(data, 0);
            var obj = node.ReadAsObject(data, 1);
            var bval = type.Value;
            var chk = new UnionNodeSummary(obj, index);

            // add or update key if our FieldCount is new or bigger than previously noted for this union type
            if (!result.TryGetValue(bval, out var c))
                result[bval] = chk;
            else
                c.UpdateWith(node, index);
        }

        return result;
    }

    public static byte[] GetUnionTypes(this FlatBufferTableObject node, ReadOnlySpan<byte> data)
    {
        var count = node.Length;
        byte[] result = new byte[count];
        for (int i = 0; i < count; i++)
        {
            var entry = node.GetEntry(i);
            var type0 = entry.ReadAs<byte>(data, 0);
            result[i] = type0.Value;
        }
        return result;
    }
}
