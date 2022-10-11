using System;
using System.Collections.Generic;

namespace FlatCrawler.Lib;

public sealed class FlatBufferUnionInfo
{
    public Dictionary<byte, FBType> UnionTypes { get; }

    public FlatBufferUnionInfo() : this(new()) { }
    public FlatBufferUnionInfo(Dictionary<byte, FBType> info) => UnionTypes = info;

    public FlatBufferUnionNode ReadUnion(FlatBufferNodeField parent, ReadOnlySpan<byte> data)
    {
        var type = parent.ReadAs<byte>(data, 0).Value;
        var node = parent.ReadAsObject(data, 1);
        return ReadUnion(node, data, type);
    }

    public FlatBufferUnionNode ReadUnion(FlatBufferObject node, ReadOnlySpan<byte> data, byte type)
    {
        var info = UnionTypes[type];

        var inner = node.FieldInfo.IsArray ? node.ReadArrayAs(data, 0, info.Type) : node.ReadAs(data, 0, info.Type);
        var unionFieldInfo = node.FieldInfo with { Type = info };
        return new(unionFieldInfo, node, inner);
    }
}
