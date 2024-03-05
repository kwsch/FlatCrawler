using System;
using System.Collections.Generic;
using System.Linq;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

/// <summary>
/// Node that contains a serialized schema object array.
/// </summary>
public sealed record FlatBufferTableObject : FlatBufferTable<FlatBufferObject>
{
    public const int HeaderSize = 4;
    public const int EntrySize = 4;

    private DataRange NodePtrMemory => new(FieldOffset..(FieldOffset + Size), DataCategory.Pointer, () => $"{TypeName} Ptr ({Name} @ 0x{Offset:X})", true);
    private DataRange TableMemory => new(Offset..(DataTableOffset + (Length * EntrySize)), DataCategory.Value, () => $"{TypeName} Data");
    private DataRange ArrayLengthMemory => new(Offset..(Offset + HeaderSize), DataCategory.Misc, () => $"Array Length ({Length})", true);
    private DataRange ObjectArrayMemory => new(DataTableOffset..(DataTableOffset + (Length * EntrySize)), DataCategory.Pointer, () => $"{TypeName} Ptrs", true);

    /// <summary>
    /// Absolute offset that has the raw table pointer bytes.
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

    /// <summary>
    /// Disassociates child nodes from each other depending on their union type.
    /// </summary>
    public void InterpretAsUnionTable()
    {
        var file = FbFile;
        var data = file.Data;
        var unionTypeClasses = new Dictionary<byte, FBFieldInfo>();
        foreach (var entry in Entries)
        {
            var typeNode = entry.ReadNodeAndTrack(0, data, TypeCode.Byte, false);
            var type = ((FlatBufferFieldValue<byte>)typeNode).Value;
            typeNode.Name = "UnionType";

            var objectNode = entry.ReadNodeAndTrack(1, data, TypeCode.Object, false);
            var child = (FlatBufferObject)objectNode;
            var name = $"Union{type}";
            if (!unionTypeClasses.TryGetValue(type, out var fieldInfo))
            {
                var newUnionInfo = new FBFieldInfo
                {
                    Name = name,
                    Size = child.FieldInfo.Size,
                    Type = new FBClass(file) { TypeName = name },
                };
                unionTypeClasses.Add(type, fieldInfo = newUnionInfo);
            }
            child.TrackFieldInfo(fieldInfo);
            child.Name = name;

            entry.Name = name;
        }

        var types = unionTypeClasses.Select(z => z.Key).OrderBy(z => z);
        Entries[0].TypeName = $"Union{{{string.Join(',', types)}}}";
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
