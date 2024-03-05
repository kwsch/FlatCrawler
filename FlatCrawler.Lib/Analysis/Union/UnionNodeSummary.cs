using System.Collections.Generic;

namespace FlatCrawler.Lib;

public sealed class UnionNodeSummary
{
    public readonly List<(FlatBufferObject Node, int Index)> Entries = [];
    public FlatBufferObject NodeWithMostFields { get; private set; }

    public UnionNodeSummary(FlatBufferObject node, int index)
    {
        NodeWithMostFields = node;
        Entries.Add((node, index));
    }

    public void UpdateWith(FlatBufferObject node, int index)
    {
        if (node.FieldCount > NodeWithMostFields.FieldCount)
            NodeWithMostFields = node;
        Entries.Add((node, index));
    }
}
