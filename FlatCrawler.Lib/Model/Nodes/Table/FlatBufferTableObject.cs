using System;
using System.Linq;

namespace FlatCrawler.Lib;

public sealed record FlatBufferTableObject : FlatBufferTable<FlatBufferObject>
{
    public const int HeaderSize = 4;
    public const int EntrySize = 4;

    private string _name = "???";
    private string _typeName = "Object[]";

    private FlatBufferTableClass ObjectClass { get; set; } = null!; // will always be set by static initializer

    public override string TypeName
    {
        get => _typeName;
        set
        {
            _typeName = $"{value}[]";
            foreach (var e in Entries)
                e.TypeName = value;
        }
    }

    public override string Name
    {
        get => _name;
        set
        {
            _name = value;
            for (int i = 0; i < Entries.Length; ++i)
                Entries[i].Name = $"{value}[{i}]";
        }
    }

    public override FlatBufferObject GetEntry(int entryIndex) => Entries[entryIndex];

    private FlatBufferTableObject(int offset, int length, FlatBufferNode parent, int dataTableOffset) :
        base(offset, parent, length, dataTableOffset)
    { }

    private int GetEntryFieldCountMax() => Entries.Max(x => x.VTable.FieldInfo.Length);

    private void ReadArray(byte[] data)
    {
        for (int i = 0; i < Entries.Length; i++)
            Entries[i] = GetEntryAtIndex(data, i);
    }

    private FlatBufferObject GetEntryAtIndex(byte[] data, int entryIndex)
    {
        var arrayEntryPointerOffset = DataTableOffset + (entryIndex * EntrySize);
        var dataTablePointerShift = BitConverter.ToInt32(data, arrayEntryPointerOffset);
        var dataTableOffset = arrayEntryPointerOffset + dataTablePointerShift;

        return FlatBufferObject.Read(arrayEntryPointerOffset, this, data, dataTableOffset);
    }

    public static FlatBufferTableObject Read(int offset, FlatBufferNode parent, int fieldIndex, byte[] data)
    {
        int length = BitConverter.ToInt32(data, offset);
        var node = new FlatBufferTableObject(offset, length, parent, offset + HeaderSize);
        node.ReadArray(data);

        // If this table is part of another table, link the ObjectTypes (class) reference
        // If not, then just set up placeholder data.
        if (parent.Parent is not FlatBufferTableObject t)
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

        node.ObjectClass.MemberTypeChanged += node.ObjectClass_MemberTypeChanged;

        return node;
    }

    public static FlatBufferTableObject Read(FlatBufferNodeField parent, int fieldIndex, byte[] data) => Read(parent.GetReferenceOffset(fieldIndex, data), parent, fieldIndex, data);

    private void ObjectClass_MemberTypeChanged(object? sender, MemberTypeChangedArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Changing Member Type: {e.MemberIndex} {e.OldType} -> {e.NewType}");
        foreach (var entry in Entries)
        {
            if (entry.HasField(e.MemberIndex))
                entry.UpdateNodeType(e.MemberIndex, CommandUtil.Data.ToArray(), e.NewType.Type, e.NewType.IsArray);
        }
    }

    public void OnFieldTypeChanged(int fieldIndex, TypeCode code, bool asArray, FlatBufferNode source)
    {
        System.Diagnostics.Debug.WriteLine($"Changing Field Type: {fieldIndex} {source.TypeName} -> {code} {asArray}");
        ObjectClass.SetMemberType(fieldIndex, code, asArray);
    }
}
