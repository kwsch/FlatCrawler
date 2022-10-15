using System;
using System.Diagnostics;

namespace FlatCrawler.Lib;

/// <summary>
/// Node that contains a serialized schema object.
/// </summary>
/// <remarks>For an array (table), see <see cref="FlatBufferTable{T}"/></remarks>
public sealed record FlatBufferObject : FlatBufferNodeField, ISchemaObserver
{
    public FBClass ObjectClass => (FBClass)FieldInfo.Type;

    private FlatBufferObject(int offset, VTable vTable, int dataTableOffset, FlatBufferNode parent) :
        base(offset, vTable, dataTableOffset, parent)
    {
        FieldInfo = new FBFieldInfo { Type = new FBClass() };
        RegisterObjectClass();
    }

    ~FlatBufferObject()
    {
        UnRegisterObjectClass();
    }

    public override void TrackChildFieldNode(int fieldIndex, ReadOnlySpan<byte> data, TypeCode code, bool asArray, FlatBufferNode node)
    {
        ObjectClass.SetMemberType(fieldIndex, data, code, asArray);

        Fields[fieldIndex] = node;
        node.TrackFieldInfo(ObjectClass.Members[fieldIndex]);
    }

    public override void TrackFieldInfo(FBFieldInfo sharedInfo)
    {
        UnRegisterObjectClass();
        FieldInfo = sharedInfo;
        RegisterObjectClass();
    }

    public override void TrackType(FBType classType)
    {
        UnRegisterObjectClass();
        FieldInfo = FieldInfo with { Type = classType };
        RegisterObjectClass();
    }

    /// <summary>
    /// Update all data based on the ObjectClass
    /// </summary>
    private void RegisterObjectClass()
    {
        ObjectClass.AssociateVTable(VTable);

        Fields = new FlatBufferNode[ObjectClass.Members.Count];

        ObjectClass.Observers.Add(this);
    }

    /// <summary>
    /// Remove event associations
    /// </summary>
    private void UnRegisterObjectClass()
    {
        ObjectClass.Observers.Remove(this);
    }

    public void OnMemberCountChanged(int count)
    {
        if (Fields.Length == count)
            return;
        var tmp = new FlatBufferNode[count];
        Fields.CopyTo(tmp, 0);
        Fields = tmp;
    }

    public void OnMemberTypeChanged(MemberTypeChangedArgs e)
    {
        Debug.WriteLine($"Changing Member Type: {e.MemberIndex} {e.OldType} -> {e.NewType}");
        if (HasField(e.MemberIndex))
        {
            var node = ReadNode(e.MemberIndex, e.Data, e.NewType.Type, e.FieldInfo.IsArray);
            Fields[e.MemberIndex] = node;
            node.TrackFieldInfo(e.FieldInfo);
        }
    }

    public static FlatBufferObject Read(int offset, FlatBufferNode parent, ReadOnlySpan<byte> data)
    {
        int tableOffset = offset;
        return Read(offset, parent, data, tableOffset);
    }

    public static FlatBufferObject Read(int offset, FlatBufferNode parent, ReadOnlySpan<byte> data, int tableOffset)
    {
        // Read VTable
        var vTableOffset = GetVtableOffset(tableOffset, data, true);
        var vTable = ReadVTable(vTableOffset, data);

        // Ensure VTable is correct
        if (vTableOffset < tableOffset && (vTableOffset + vTable.VTableLength) > tableOffset)
            throw new IndexOutOfRangeException("VTable overflows into Data Table. Not a valid VTable.");
        return new FlatBufferObject(offset, vTable, tableOffset, parent);
    }

    public static FlatBufferObject Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data)
    {
        var offset = parent.GetReferenceOffset(fieldIndex, data);
        return Read(offset, parent, data);
    }
}
