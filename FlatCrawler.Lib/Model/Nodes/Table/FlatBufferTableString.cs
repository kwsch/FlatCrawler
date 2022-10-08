using System;

namespace FlatCrawler.Lib;

public sealed record FlatBufferTableString : FlatBufferTable<FlatBufferStringValue>
{
    public override string TypeName { get => "string[]"; set { } }
    public override FlatBufferNode GetEntry(int entryIndex) => Entries[entryIndex];

    private FlatBufferTableString(int offset, int length, FlatBufferNode parent, int dataTableOffset) : base(offset, parent, length, dataTableOffset)
    {
    }

    private void ReadArray(byte[] data)
    {
        for (int i = 0; i < Entries.Length; i++)
            Entries[i] = GetEntryAtIndex(data, i);
    }

    private FlatBufferStringValue GetEntryAtIndex(byte[] data, int entryIndex)
    {
        var arrayEntryPointerOffset = DataTableOffset + (entryIndex * 4);
        return FlatBufferStringValue.Read(arrayEntryPointerOffset, this, data);
    }

    public static FlatBufferTableString Read(int offset, FlatBufferNode parent, byte[] data)
    {
        int length = BitConverter.ToInt32(data, offset);
        var node = new FlatBufferTableString(offset, length, parent, offset + 4);
        node.ReadArray(data);
        return node;
    }

    public static FlatBufferTableString Read(FlatBufferNodeField parent, int fieldIndex, byte[] data) => Read(parent.GetReferenceOffset(fieldIndex, data), parent, data);
}
