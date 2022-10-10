using System;
using System.Linq;

namespace FlatCrawler.Lib;

// Object[] Ptr field type
public sealed record FlatBufferTableObject : FlatBufferTable<FlatBufferObject>
{
    public const int HeaderSize = 4;
    public const int EntrySize = 4;

    private string _name = "???";
    private string _typeName = "Object[]";

    public FBClass ObjectClass => (FBClass)FieldInfo.Type;

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
    {
        FieldInfo = new FBFieldInfo { Type = new FBClass(), IsArray = true };
    }

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

        var entry = FlatBufferObject.Read(arrayEntryPointerOffset, this, data, dataTableOffset);

        // Override entry type with table's class type
        entry.SetClassType(ObjectClass);

        return entry;
    }

    public static FlatBufferTableObject Read(int offset, FlatBufferNode parent, int fieldIndex, byte[] data)
    {
        int length = BitConverter.ToInt32(data, offset);
        var node = new FlatBufferTableObject(offset, length, parent, offset + HeaderSize);
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

    public static FlatBufferTableObject Read(FlatBufferNodeField parent, int fieldIndex, byte[] data) => Read(parent.GetReferenceOffset(fieldIndex, data), parent, fieldIndex, data);
}
