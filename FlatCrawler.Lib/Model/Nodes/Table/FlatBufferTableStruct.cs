using System;
using System.Runtime.InteropServices;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

/// <summary>
/// Node that contains a serialized value type array.
/// </summary>
/// <inheritdoc cref="FlatBufferFieldValue&lt;T&gt;"/>
public sealed record FlatBufferTableStruct<T> : FlatBufferTable<FlatBufferFieldValue<T>> where T : struct
{
    public override string TypeName { get => $"{ArrayType}[]"; set { } }

    /// <summary> The type of the array elements, <typeparamref name="T"/>. </summary>
    public TypeCode ArrayType { get; }

    private FlatBufferTableStruct(int offset, int length, FlatBufferNode parent, int dataTableOffset, TypeCode typeCode) : base(offset, parent, length, dataTableOffset)
    {
        ArrayType = typeCode;
    }

    protected override FlatBufferFieldValue<T> GetEntryAtIndex(ReadOnlySpan<byte> data, int entryIndex)
    {
        var offset = DataTableOffset + (entryIndex * Marshal.SizeOf<T>());
        return FlatBufferFieldValue<T>.Read(offset, this, data, ArrayType);
    }

    /// <summary>
    /// Exports all values from this node to a new array.
    /// </summary>
    public T[] ToArray()
    {
        var result = new T[Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = Entries[i].Value;
        return result;
    }

    /// <summary>
    /// Gets the node at the specified index.
    /// </summary>
    /// <param name="entryIndex">The index of the node to get.</param>
    /// <returns>The node at the specified index.</returns>
    public override FlatBufferNode GetEntry(int entryIndex) => Entries[entryIndex];

    /// <summary>
    /// Reads a new table node from the specified data.
    /// </summary>
    public static FlatBufferTableStruct<T> Read(int offset, FlatBufferNode parent, ReadOnlySpan<byte> data, TypeCode type)
    {
        int length = ReadInt32LittleEndian(data[offset..]);
        var node = new FlatBufferTableStruct<T>(offset, length, parent, offset + 4, type);
        node.ReadArray(data);
        return node;
    }

    /// <summary>
    /// Reads a new table node from the specified data.
    /// </summary>
    /// <param name="parent">The parent node.</param>
    /// <param name="fieldIndex">The index of the field in the parent node.</param>
    /// <param name="data">The data to read from.</param>
    /// <param name="type">The type of the array.</param>
    /// <returns>New child node.</returns>
    public static FlatBufferTableStruct<T> Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data, TypeCode type) => Read(parent.GetReferenceOffset(fieldIndex, data), parent, data, type);
}
