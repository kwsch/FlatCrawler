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
    public const int HeaderSize = sizeof(int);
    public readonly int EntrySize = Marshal.SizeOf<T>();

    private DataRange NodePtrMemory => new(FieldOffset..(FieldOffset + Size), $"{TypeName} Ptr (@ 0x{Offset:X})", true);
    private DataRange TableMemory => new(Offset..(DataTableOffset + Length * EntrySize), $"{TypeName} Data");
    private DataRange ArrayLengthMemory => new(Offset..(Offset + HeaderSize), $"Array Length ({Length})", true);
    private DataRange ObjectArrayMemory => new(DataTableOffset..(DataTableOffset + Length * EntrySize), $"{TypeName} Ptrs", true);

    public override string TypeName { get => $"{ArrayType}[]"; set { } }

    /// <summary> The type of the array elements, <typeparamref name="T"/>. </summary>
    public TypeCode ArrayType { get; }

    /// <summary>
    /// Absolute offset that has the raw table pointer bytes.
    /// </summary>
    public int FieldOffset { get; }

    private FlatBufferTableStruct(int fieldOffset, int arrayOffset, int length, FlatBufferNode parent, int dataTableOffset, TypeCode typeCode) :
        base(arrayOffset, parent, length, dataTableOffset)
    {
        FieldOffset = fieldOffset;
        ArrayType = typeCode;
    }

    public override void RegisterMemory()
    {
        FbFile.SetProtectedMemory(NodePtrMemory);
        FbFile.SetProtectedMemory(TableMemory);
        FbFile.SetProtectedMemory(ArrayLengthMemory);
        FbFile.SetProtectedMemory(ObjectArrayMemory);

        foreach (var entry in Entries)
            entry.RegisterMemory();
    }

    public override void UnRegisterMemory()
    {
        FbFile.RemoveProtectedMemory(NodePtrMemory);
        FbFile.RemoveProtectedMemory(TableMemory);
        FbFile.RemoveProtectedMemory(ArrayLengthMemory);
        FbFile.RemoveProtectedMemory(ObjectArrayMemory);

        foreach (var entry in Entries)
            entry.UnRegisterMemory();
    }

    protected override FlatBufferFieldValue<T> GetEntryAtIndex(ReadOnlySpan<byte> data, int entryIndex)
    {
        var offset = DataTableOffset + (entryIndex * EntrySize);
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

    public static long GetSize(long length) => length * Marshal.SizeOf<T>();

    public static long PeekSize(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data)
    {
        var offset = parent.GetReferenceOffset(fieldIndex, data);
        if (offset % 4 != 0)
            return long.MaxValue;
        var length = ReadInt32LittleEndian(data[offset..]);
        return GetSize(length);
    }

    /// <summary>
    /// Reads a new table node from the specified data.
    /// </summary>
    public static FlatBufferTableStruct<T> Read(int offset, FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data, TypeCode type)
    {
        var arrayOffset = parent.GetReferenceOffset(fieldIndex, data);
        int length = ReadInt32LittleEndian(data[arrayOffset..]);
        var dataTableOffset = arrayOffset + HeaderSize;

        if (GetSize(length) > (data.Length - dataTableOffset))
            throw new ArgumentException("The specified data is too short to contain the specified array.", nameof(data));

        var node = new FlatBufferTableStruct<T>(offset, arrayOffset, length, parent, dataTableOffset, type);
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
    public static FlatBufferTableStruct<T> Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data, TypeCode type) => Read(parent.GetFieldOffset(fieldIndex), parent, fieldIndex, data, type);
}
