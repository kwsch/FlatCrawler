using System;
using System.Collections.Generic;

namespace FlatCrawler.Lib;

public sealed record FlatBufferUnionNodeSummary(byte Type, int Index, int FieldCount)
{
    public static Dictionary<byte, FlatBufferUnionNodeSummary> GetUnionAnalysis(IArrayNode array, byte[] data)
        => FlatBufferUnionNodeSummaries(array.Entries, data);

    public static Dictionary<byte, FlatBufferUnionNodeSummary> FlatBufferUnionNodeSummaries(IReadOnlyList<FlatBufferNode> entries, byte[] data)
    {
        var result = new Dictionary<byte, FlatBufferUnionNodeSummary>();
        for (var index = 0; index < entries.Count; index++)
        {
            var flatBufferNode = entries[index];
            var node = (FlatBufferObject)flatBufferNode;
            var type = node.GetFieldValue(0, data, TypeCode.Byte);
            var obj = node.ReadObject(1, data);
            var bval = ((FlatBufferFieldValue<byte>)type).Value;
            var chk = new FlatBufferUnionNodeSummary(bval, index, obj.FieldCount);
            node.TypeName = chk.ToString();

            // add or update key if our FieldCount is new or bigger than previously noted for this union type
            if (!result.TryGetValue(bval, out var c) || c.FieldCount < chk.FieldCount)
                result[bval] = chk;
        }

        return result;
    }
}
