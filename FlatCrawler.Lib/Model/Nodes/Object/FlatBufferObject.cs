using System;
using System.Diagnostics;

namespace FlatCrawler.Lib;

public sealed record FlatBufferObject : FlatBufferNodeField
{
    public FBClass ObjectClass => (FBClass)FieldInfo.Type;

    /*{
        get => (FBClass)FieldInfo.Type;
        private set
        {
            FieldInfo = value;
            Fields = new FlatBufferNode[_ObjectClass.Members.Count];
            _ObjectClass.MemberTypeChanged += OnMemberTypeChanged;
            _ObjectClass.MemberCountChanged += OnMemberCountChanged;
        }
    }*/

    public override string TypeName { get; set; } = "Object";

    private FlatBufferObject(int offset, VTable vTable, int dataTableOffset, FlatBufferNode parent) :
        base(offset, vTable, dataTableOffset, parent)
    {
        FieldInfo = new FBFieldInfo { Type = new FBClass() };
        ObjectClass.AssociateVTable(vTable);
    }

    ~FlatBufferObject()
    {
        ObjectClass.MemberTypeChanged -= OnMemberTypeChanged;
        ObjectClass.MemberCountChanged -= OnMemberCountChanged;
    }

    public override void TrackChildFieldNode(int fieldIndex, TypeCode code, bool asArray, FlatBufferNode node)
    {
        ObjectClass.SetMemberType(fieldIndex, code, asArray);
        Fields[fieldIndex] = node;

        // Override local field info with tracked member info
        node.FieldInfo = ObjectClass.Members[fieldIndex];
    }

    private void OnMemberCountChanged(object? sender, int e)
    {
        Fields = new FlatBufferNode[e];
    }

    private void OnMemberTypeChanged(object? sender, MemberTypeChangedArgs e)
    {
        Debug.WriteLine($"Changing Member Type: {e.MemberIndex} {e.OldType} -> {e.NewType}");
        if (HasField(e.MemberIndex))
            UpdateNodeType(e.MemberIndex, CommandUtil.Data.ToArray(), e.NewType.Type, e.FieldInfo.IsArray);
    }

    public static FlatBufferObject Read(int offset, FlatBufferNode parent, byte[] data)
    {
        int tableOffset = offset;
        return Read(offset, parent, data, tableOffset);
    }

    public static FlatBufferObject Read(int offset, FlatBufferNode parent, byte[] data, int tableOffset)
    {
        // Read VTable
        var vTableOffset = GetVtableOffset(tableOffset, data, true);
        var vTable = ReadVTable(vTableOffset, data);

        // Ensure VTable is correct
        if (vTableOffset < tableOffset && (vTableOffset + vTable.VTableLength) > tableOffset)
            throw new IndexOutOfRangeException("VTable overflows into Data Table. Not a valid VTable.");
        return new FlatBufferObject(offset, vTable, tableOffset, parent);
    }

    public static FlatBufferObject Read(FlatBufferNodeField parent, int fieldIndex, byte[] data)
    {
        var offset = parent.GetReferenceOffset(fieldIndex, data);
        return Read(offset, parent, data);
    }
}
