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

    public FBClass ObjectClass => (FBClass)FieldInfo.Type;

    public override string TypeName
    {
        get => $"{ObjectClass.TypeName}[{Entries.Length}]";
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

    private FlatBufferTableObject(int offset, int length, FlatBufferNode parent, int dataTableOffset) :
        base(offset, parent, length, dataTableOffset)
    {
        FieldInfo = new FBFieldInfo { Type = new FBClass(FbFile), Size = EntrySize, IsArray = true };
    }

    protected override FlatBufferObject GetEntryAtIndex(ReadOnlySpan<byte> data, int entryIndex)
    {
        var arrayEntryPointerOffset = DataTableOffset + (entryIndex * EntrySize);
        var dataTablePointerShift = ReadInt32LittleEndian(data[arrayEntryPointerOffset..]);
        var dataTableOffset = arrayEntryPointerOffset + dataTablePointerShift;

        var entry = FlatBufferObject.Read(arrayEntryPointerOffset, this, data, dataTableOffset);

        // Override entry type with table's class type
        entry.TrackType(ObjectClass);

        return entry;
    }

    public static int GetSize(int length) => length * (EntrySize + sizeof(int)); // bare minimum rough guess, considering vtable

    /// <summary>
    /// Reads a new table node from the specified data.
    /// </summary>
    public static FlatBufferTableObject Read(int offset, FlatBufferNode parent, int fieldIndex, ReadOnlySpan<byte> data)
    {
        int length = ReadInt32LittleEndian(data[offset..]);
        var dataTableOffset = offset + HeaderSize;
        if (GetSize(length) > data.Length - dataTableOffset)
            throw new ArgumentException("The specified data is too short to contain the specified array.", nameof(data));

        var node = new FlatBufferTableObject(offset, length, parent, dataTableOffset);
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
    public static FlatBufferTableObject Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data) => Read(parent.GetReferenceOffset(fieldIndex, data), parent, fieldIndex, data);
}
