using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

/// <summary>
/// Node that contains a serialized schema object array.
/// </summary>
public sealed record FlatBufferTableObject : FlatBufferTable<FlatBufferObject>
{
    public const int HeaderSize = 4;
    public const int EntrySize = 4;

    private DataRange NodePtrMemory => new(FieldOffset..(FieldOffset + Size), $"{TypeName} Ptr (@ 0x{Offset:X})", true);
    private DataRange TableMemory => new(Offset..(DataTableOffset + Length * EntrySize), $"{TypeName} Data");
    private DataRange ArrayLengthMemory => new(Offset..(Offset + HeaderSize), $"Array Length ({Length})", true);
    private DataRange ObjectArrayMemory => new(DataTableOffset..(DataTableOffset + Length * EntrySize), $"{TypeName} Ptrs", true);

    /// <summary>
    /// Absolute offset that has the raw string bytes.
    /// </summary>
    public int FieldOffset { get; }

    public FBClass ObjectClass => (FBClass)FieldInfo.Type;

    public override string TypeName
    {
        get => $"{ObjectClass.TypeName}[{Length}]";
        set => ObjectClass.TypeName = value;
    }

    public override string Name
    {
        get => FieldInfo.Name;
        set
        {
            FieldInfo.Name = value;
            for (int i = 0; i < Entries.Length; ++i)
                Entries[i].Name = $"{value}[{i}]";
        }
    }

    public override FlatBufferObject GetEntry(int entryIndex) => Entries[entryIndex];
    public int GetEntryWithField(int fieldIndex) => Array.FindIndex(Entries, e => e.HasField(fieldIndex));

    private FlatBufferTableObject(int fieldOffset, int arrayOffset, int length, FlatBufferNode parent, int dataTableOffset) :
        base(arrayOffset, parent, length, dataTableOffset)
    {
        FieldOffset = fieldOffset;
        FieldInfo = new FBFieldInfo { Type = new FBClass(FbFile), Size = EntrySize, IsArray = true };
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

    public override void TrackFieldInfo(FBFieldInfo sharedInfo)
    {
        FieldInfo = sharedInfo;

        // Override entry type with table's new class type
        foreach (var entry in Entries)
            entry.TrackType(FieldInfo);
    }

    protected override FlatBufferObject GetEntryAtIndex(ReadOnlySpan<byte> data, int entryIndex)
    {
        var arrayEntryPointerOffset = DataTableOffset + (entryIndex * EntrySize);
        var dataTablePointerShift = ReadInt32LittleEndian(data[arrayEntryPointerOffset..]);
        var dataTableOffset = arrayEntryPointerOffset + dataTablePointerShift;

        return FlatBufferObject.Read(arrayEntryPointerOffset, this, data, dataTableOffset);
    }

    public static int GetSize(int length) => (length * EntrySize) + HeaderSize; // bare minimum rough guess, considering vtable

    /// <summary>
    /// Reads a new table node from the specified data.
    /// </summary>
    public static FlatBufferTableObject Read(int offset, FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data)
    {
        var arrayOffset = parent.GetReferenceOffset(fieldIndex, data);
        int length = ReadInt32LittleEndian(data[arrayOffset..]);
        var dataTableOffset = arrayOffset + HeaderSize;
        if (GetSize(length) > (data.Length - dataTableOffset))
            throw new ArgumentException("The specified data is too short to contain the specified array.", nameof(data));

        var node = new FlatBufferTableObject(offset, arrayOffset, length, parent, dataTableOffset);
        node.ReadArray(data);

        // If this table is part of another table, link the ObjectTypes (class) reference
        // If not, then just set up placeholder data.
        /*if (parent.Parent is not FlatBufferTableObject t)
        {
            int memberCount = node.GetEntryFieldCountMax();
            node.ObjectClass = new FlatBufferTableClass(memberCount);
        }
        else if (t.ObjectClass.TryGetSubClass(fieldIndex, out var subClass))
        {
            node.ObjectClass = subClass;
        }
        else
        {
            int memberCount = node.GetEntryFieldCountMax();
            node.ObjectClass = t.ObjectClass.RegisterSubClass(fieldIndex, memberCount);
        }

        node.ObjectClass.MemberTypeChanged += node.ObjectClass_MemberTypeChanged;*/

        return node;
    }

    /// <summary>
    /// Reads a new table node from the specified data.
    /// </summary>
    /// <param name="parent">The parent node.</param>
    /// <param name="fieldIndex">The index of the field in the parent node.</param>
    /// <param name="data">The data to read from.</param>
    /// <returns>New child node.</returns>
    public static FlatBufferTableObject Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data) => Read(parent.GetFieldOffset(fieldIndex), parent, fieldIndex, data);
}
