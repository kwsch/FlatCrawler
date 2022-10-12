using System;
using System.Collections.Generic;

namespace FlatCrawler.Lib;

/// <summary>
/// Abstract Node that contains a serialized array.
/// </summary>
/// <param name="Offset">Absolute Offset of the node from the start of the FlatBuffer file.</param>
/// <param name="Parent">Parent that owns this child node.</param>
/// <param name="Length">Count of nodes in the array.</param>
/// <param name="DataTableOffset">Absolute offset to the Serialized array data.</param>
public abstract record FlatBufferTable<T>(int Offset, FlatBufferNode Parent, int Length, int DataTableOffset)
    : FlatBufferNode(Offset, Parent), IArrayNode where T : FlatBufferNode
{
    /// <summary>
    /// All child nodes
    /// </summary>
    public T[] Entries { get; } = new T[Length];

    IReadOnlyList<FlatBufferNode> IArrayNode.Entries => Entries;

    public abstract FlatBufferNode GetEntry(int entryIndex);

    public override int GetChildIndex(FlatBufferNode? child)
    {
        if (child is null)
            return -1;
        return Array.FindIndex(Entries, z => ReferenceEquals(z, child));
    }

    /// <summary>
    /// Instantiates all nodes from the specified data.
    /// </summary>
    /// <param name="data"></param>
    protected void ReadArray(ReadOnlySpan<byte> data)
    {
        for (int i = 0; i < Entries.Length; i++)
            Entries[i] = GetEntryAtIndex(data, i);
    }

    /// <summary>
    /// Instantiates a node from the specified data.
    /// </summary>
    protected abstract T GetEntryAtIndex(ReadOnlySpan<byte> data, int entryIndex);
}
