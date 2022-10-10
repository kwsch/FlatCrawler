using System;
using System.Collections.Generic;
using System.Linq;

namespace FlatCrawler.Lib;

public static class UnionAnalysis
{
    public static UnionAnalysisResult AnalyzeUnion(this IArrayNode array, ReadOnlySpan<byte> data)
    {
        var result = GetUnionAnalysis(array, data);
        var grouped = result
            .GroupBy(z => z.Key)
            .ToDictionary(z => z.Key, z => z.Select(x => x.Value).ToArray());

        return new UnionAnalysisResult(grouped);
    }

    private static Dictionary<byte, FlatBufferUnionNodeSummary> GetUnionAnalysis(IArrayNode array, ReadOnlySpan<byte> data)
        => FlatBufferUnionNodeSummaries(array.Entries, data);

    private static Dictionary<byte, FlatBufferUnionNodeSummary> FlatBufferUnionNodeSummaries(IReadOnlyList<FlatBufferNode> entries, ReadOnlySpan<byte> data)
    {
        var result = new Dictionary<byte, FlatBufferUnionNodeSummary>();
        for (var index = 0; index < entries.Count; index++)
        {
            var flatBufferNode = entries[index];
            var node = (FlatBufferObject)flatBufferNode;
            var type = node.GetFieldValue(0, data, TypeCode.Byte);
            var obj = node.ReadObject(1, data);
            var bval = ((FlatBufferFieldValue<byte>)type).Value;
            var chk = new FlatBufferUnionNodeSummary(bval, index, obj);
            node.TypeName = chk.ToString();

            // add or update key if our FieldCount is new or bigger than previously noted for this union type
            if (!result.TryGetValue(bval, out var c) || c.FieldCount < chk.FieldCount)
                result[bval] = chk;
        }

        return result;
    }
}

// ReSharper disable once NotAccessedPositionalProperty.Global
public sealed record FlatBufferUnionNodeSummary(byte Type, int Index, FlatBufferObject Node)
{
    public int FieldCount => Node.FieldCount;
}

public sealed record UnionAnalysisResult(Dictionary<byte, FlatBufferUnionNodeSummary[]> Groups)
{
    public byte[] UniqueTypeCodes => Groups.Keys.OrderBy(z => z).ToArray();
    public IEnumerable<int> GetIndexes(byte typeCode) => Groups[typeCode].Select(z => z.Index);
    public int MaxFieldCount(byte typeCode) => Groups[typeCode].Max(z => z.FieldCount);
    public bool SameFieldCount(byte typeCode) => Groups[typeCode].Select(z => z.FieldCount).Distinct().Count() == 1;
}
