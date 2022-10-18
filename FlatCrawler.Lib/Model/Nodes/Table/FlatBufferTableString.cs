using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

/// <summary>
/// Node that contains a serialized string array.
/// </summary>
public sealed record FlatBufferTableString : FlatBufferTable<FlatBufferStringValue>
{
    public override string TypeName { get => "string[]"; set { } }
    public bool IsReadable => Array.TrueForAll(Entries, x => x.IsReadable);

    public override FlatBufferNode GetEntry(int entryIndex) => Entries[entryIndex];

    private FlatBufferTableString(int offset, int length, FlatBufferNode parent, int dataTableOffset) : base(offset, parent, length, dataTableOffset)
    {
    }

    protected override FlatBufferStringValue GetEntryAtIndex(ReadOnlySpan<byte> data, int entryIndex)
    {
        var arrayEntryPointerOffset = DataTableOffset + (entryIndex * 4);
        return FlatBufferStringValue.Read(arrayEntryPointerOffset, this, data);
    }

    /// <summary>
    /// Exports all values from this node to a new array.
    /// </summary>
    public string[] ToArray()
    {
        var result = new string[Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = Entries[i].Value;
        return result;
    }

    /// <summary>
    /// Reads a new table node from the specified data.
    /// </summary>
    public static FlatBufferTableString Read(int offset, FlatBufferNode parent, ReadOnlySpan<byte> data)
    {
        int length = ReadInt32LittleEndian(data[offset..]);
        var node = new FlatBufferTableString(offset, length, parent, offset + 4);
        node.ReadArray(data);
        return node;
    }

    /// <summary>
    /// Reads a new table node from the specified data.
    /// </summary>
    /// <param name="parent">The parent node.</param>
    /// <param name="fieldIndex">The index of the field in the parent node.</param>
    /// <param name="data">The data to read from.</param>
    /// <returns>New child node.</returns>
    public static FlatBufferTableString Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data) => Read(parent.GetReferenceOffset(fieldIndex, data), parent, data);
}
