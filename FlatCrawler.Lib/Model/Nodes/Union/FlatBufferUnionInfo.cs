using System.Collections.Generic;

namespace FlatCrawler.Lib;

public sealed class FlatBufferUnionInfo
{
    public Dictionary<byte, FBType> UnionTypes { get; }

    public FlatBufferUnionInfo() : this(new()) { }
    public FlatBufferUnionInfo(Dictionary<byte, FBType> info) => UnionTypes = info;

    public FlatBufferUnionNode ReadUnion(FlatBufferNodeField parent, byte[] data)
    {
        var type = parent.ReadUInt8(0, data).Value;
        var node = parent.ReadObject(1, data);
        return ReadUnion(node, data, type);
    }

    public FlatBufferUnionNode ReadUnion(FlatBufferObject node, byte[] data, byte type)
    {
        var info = UnionTypes[type];
        var inner = node.ReadNode(0, data, info.Type, node.FieldInfo.IsArray);
        var unionFieldInfo = node.FieldInfo with { Type = info };
        return new(unionFieldInfo, node, inner);
    }
}
