using System;
using System.Collections.Generic;

namespace FlatCrawler.Lib;

public abstract record FlatBufferTable<T>(int Offset, FlatBufferNode Parent, int Length, int DataTableOffset)
    : FlatBufferNode(Offset, Parent), IArrayNode where T : FlatBufferNode
{
    public T[] Entries { get; } = new T[Length];

    IReadOnlyList<FlatBufferNode> IArrayNode.Entries => Entries;

    public abstract FlatBufferNode GetEntry(int entryIndex);

    public override int GetChildIndex(FlatBufferNode? child)
    {
        if (child is null)
            return -1;
        return Array.FindIndex(Entries, z => ReferenceEquals(z, child));
    }
}
