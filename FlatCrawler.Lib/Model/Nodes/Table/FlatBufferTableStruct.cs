using System;
using System.Runtime.InteropServices;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

public sealed record FlatBufferTableStruct<T> : FlatBufferTable<FlatBufferFieldValue<T>> where T : struct
{
    public override string TypeName { get => $"{ArrayType}[]"; set { } }
    public TypeCode ArrayType { get; }

    private FlatBufferTableStruct(int offset, int length, FlatBufferNode parent, int dataTableOffset, TypeCode typeCode) : base(offset, parent, length, dataTableOffset)
    {
        ArrayType = typeCode;
    }

    private void ReadArray(ReadOnlySpan<byte> data)
    {
        for (int i = 0; i < Entries.Length; i++)
            Entries[i] = GetEntryAtIndex(data, i);
    }
    public T[] ToArray()
    {
        var result = new T[Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = Entries[i].Value;
        return result;
    }

    public override FlatBufferNode GetEntry(int entryIndex) => Entries[entryIndex];

    private FlatBufferFieldValue<T> GetEntryAtIndex(ReadOnlySpan<byte> data, int entryIndex)
    {
        var offset = DataTableOffset + (entryIndex * Marshal.SizeOf<T>());
        return FlatBufferFieldValue<T>.Read(offset, this, data, ArrayType);
    }

    public static FlatBufferTableStruct<T> Read(int offset, FlatBufferNode parent, ReadOnlySpan<byte> data, TypeCode type)
    {
        int length = ReadInt32LittleEndian(data[offset..]);
        var node = new FlatBufferTableStruct<T>(offset, length, parent, offset + 4, type);
        node.ReadArray(data);
        return node;
    }

    public static FlatBufferTableStruct<T> Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data, TypeCode type) => Read(parent.GetReferenceOffset(fieldIndex, data), parent, data, type);
}
