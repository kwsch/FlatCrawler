using System;
using System.Diagnostics;

namespace FlatCrawler.Lib;

/// <summary>
/// Node that contains a serialized schema object.
/// </summary>
/// <remarks>For an array (table), see <see cref="FlatBufferTable{T}"/></remarks>
public record FlatBufferObject : FlatBufferNodeField, ISchemaObserver
{
    public const int HeaderSize = sizeof(int);
    private DataRange NodePtrMemory => new(Offset..(Offset + Size), $"{TypeName} Ptr (@ 0x{DataTableOffset:X})", true);
    private DataRange DataTableMemory => new(DataTableOffset..(DataTableOffset + VTable.DataTableLength), "Data Table");
    private DataRange VTablePtrMemory => new(DataTableOffset..(DataTableOffset + HeaderSize), $"VTable Ptr (@ 0x{VTableOffset:X})", true);

    public FBClass ObjectClass => (FBClass)FieldInfo.Type;

    protected FlatBufferObject(FlatBufferFile file, VTable vTable, int offset, int dataTableOffset, FlatBufferNode? parent) :
        base(offset, vTable, dataTableOffset, parent)
    {
        FieldInfo = new FBFieldInfo { Type = new FBClass(file) };
        RegisterObjectClass();
    }

    ~FlatBufferObject()
    {
        UnRegisterObjectClass();
    }

    public override void TrackChildFieldNode(int fieldIndex, ReadOnlySpan<byte> data, TypeCode code, bool asArray, FlatBufferNode childNode)
    {
        ObjectClass.SetMemberType(this, fieldIndex, data, code, asArray);

        ref var member = ref Fields[fieldIndex];
        member?.UnRegisterMemory();

        member = childNode;
        childNode.TrackFieldInfo(ObjectClass.Members[fieldIndex]);
        childNode.RegisterMemory();
    }

    public override void RegisterMemory()
    {
        //FbFile.SetProtectedMemory(NodePtrMemory);
        FbFile.SetProtectedMemory(DataTableMemory);
        FbFile.SetProtectedMemory(VTablePtrMemory);
        ObjectClass.RegisterMemory();
    }

    public override void UnRegisterMemory()
    {
        //FbFile.RemoveProtectedMemory(NodePtrMemory);
        FbFile.RemoveProtectedMemory(DataTableMemory);
        FbFile.RemoveProtectedMemory(VTablePtrMemory);
        ObjectClass.UnRegisterMemory();
    }

    public override void TrackFieldInfo(FBFieldInfo sharedInfo)
    {
        UnRegisterObjectClass();
        FieldInfo = sharedInfo;
        RegisterObjectClass();
    }

    public override void TrackType(FBFieldInfo sharedInfo)
    {
        UnRegisterObjectClass();
        FieldInfo = FieldInfo with { Type = sharedInfo.Type, Size = sharedInfo.Size };
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
        if (!HasField(e.MemberIndex))
            return;

        ref var member = ref Fields[e.MemberIndex];
        member?.UnRegisterMemory();

        member = ReadNode(e.MemberIndex, e.Data, e.NewType.Type, e.FieldInfo.IsArray);
        member.TrackFieldInfo(e.FieldInfo);
        member.RegisterMemory();
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
        var vTable = parent.FbFile.PeekVTable(vTableOffset);

        // Ensure VTable is correct
        if (vTableOffset < tableOffset && (vTableOffset + vTable.VTableLength) > tableOffset)
            throw new IndexOutOfRangeException("VTable overflows into Data Table. Not a valid VTable.");
        return new FlatBufferObject(parent.FbFile, vTable, offset, tableOffset, parent);
    }

    public static FlatBufferObject Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data)
    {
        var offset = parent.GetReferenceOffset(fieldIndex, data);
        return Read(offset, parent, data);
    }
}
