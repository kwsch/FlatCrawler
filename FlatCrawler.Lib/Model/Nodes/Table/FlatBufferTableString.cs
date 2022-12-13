using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

/// <summary>
/// Node that contains a serialized string array.
/// </summary>
public sealed record FlatBufferTableString : FlatBufferTable<FlatBufferStringValue>
{
    public const int HeaderSize = 4;
    public const int EntrySize = 4;

    private DataRange NodePtrMemory => new(FieldOffset..(FieldOffset + Size), DataCategory.Pointer, $"{TypeName} Ptr (@ 0x{Offset:X})", true);
    private DataRange TableMemory => new(Offset..(DataTableOffset + Length * EntrySize), DataCategory.Value, $"{TypeName} Data");
    private DataRange ArrayLengthMemory => new(Offset..(Offset + HeaderSize), DataCategory.Misc, $"Array Length ({Length})", true);
    private DataRange ObjectArrayMemory => new(DataTableOffset..(DataTableOffset + Length * EntrySize), DataCategory.Pointer, $"{TypeName} Ptrs", true);

    /// <summary>
    /// Absolute offset that has the raw table pointer bytes.
    /// </summary>
    public int FieldOffset { get; }

    public override string TypeName { get => "string[]"; set { } }
    public bool IsReadable => Array.TrueForAll(Entries, x => x.IsReadable);

    public override FlatBufferNode GetEntry(int entryIndex) => Entries[entryIndex];

    private FlatBufferTableString(int fieldOffset, int arrayOffset, int length, FlatBufferNode parent, int dataTableOffset) :
        base(arrayOffset, parent, length, dataTableOffset)
    {
        FieldOffset = fieldOffset;
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

    protected override FlatBufferStringValue GetEntryAtIndex(ReadOnlySpan<byte> data, int entryIndex)
    {
        var arrayEntryPointerOffset = DataTableOffset + (entryIndex * EntrySize);
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

    public static int GetSize(int length) => (length * EntrySize) + HeaderSize; // bare minimum rough guess: ptr, len

    /// <summary>
    /// Reads a new table node from the specified data.
    /// </summary>
    public static FlatBufferTableString Read(int offset, FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data)
    {
        var arrayOffset = parent.GetReferenceOffset(fieldIndex, data);
        int length = ReadInt32LittleEndian(data[arrayOffset..]);
        var dataTableOffset = arrayOffset + HeaderSize;

        if (GetSize(length) > (data.Length - dataTableOffset))
            throw new ArgumentException("The specified data is too short to contain the specified array.", nameof(data));

        var node = new FlatBufferTableString(offset, arrayOffset, length, parent, dataTableOffset);
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
    public static FlatBufferTableString Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data) => Read(parent.GetFieldOffset(fieldIndex), parent, fieldIndex, data);
}
