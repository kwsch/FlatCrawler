using System;
using System.Collections.Generic;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

public abstract record FlatBufferNodeField : FlatBufferNode, IFieldNode
{
    public VTable VTable { get; }
    public int DataTableOffset { get; }
    public int VTableOffset => VTable.Location;

    protected FlatBufferNode?[] Fields { get; set; }
    public IReadOnlyList<FlatBufferNode?> AllFields => Fields;

    public bool HasField(int fieldIndex) => fieldIndex < VTable.FieldInfo.Length && VTable.FieldInfo[fieldIndex].HasValue;
    public int FieldCount => Fields.Length;

    protected FlatBufferNodeField(int offset, VTable vTable, int dataTableOffset, FlatBufferNode? parent = null) :
        base(offset, parent)
    {
        VTable = vTable;
        DataTableOffset = dataTableOffset;
        Fields = new FlatBufferNode[vTable.FieldInfo.Length];
    }

    public int GetFieldOffset(int fieldIndex)
    {
        var fo = VTable.FieldInfo[fieldIndex];
        if (!fo.HasValue)
            throw new ArgumentException("Field not present in Table");
        return DataTableOffset + fo.Offset;
    }

    public int GetReferenceOffset(int fieldIndex, ReadOnlySpan<byte> data)
    {
        var fieldOffset = GetFieldOffset(fieldIndex);
        var rawPtr = ReadInt32LittleEndian(data[fieldOffset..]);
        return fieldOffset + rawPtr;
    }

    public override int GetChildIndex(FlatBufferNode? child)
    {
        if (child is null)
            return -1;
        return Array.FindIndex(Fields, z => ReferenceEquals(z, child));
    }

    protected static int GetVtableOffset(int offset, ReadOnlySpan<byte> data, bool reverse = false)
    {
        var ofs = ReadInt32LittleEndian(data[offset..]);
        return reverse ? offset - ofs : offset + ofs;
    }

    protected static VTable ReadVTable(int offset, ReadOnlySpan<byte> data) => new(data, offset);

    public virtual void TrackChildFieldNode(int fieldIndex, TypeCode code, bool asArray, FlatBufferNode node)
    {
        Fields[fieldIndex] = node;
    }

    public FlatBufferNode ReadNode(int fieldIndex, ReadOnlySpan<byte> data, TypeCode type, bool asArray)
    {
        if (asArray)
            return ReadArrayAs(data, fieldIndex, type);
        return ReadAs(data, fieldIndex, type);
    }

    public FlatBufferTableObject ReadAsTable(ReadOnlySpan<byte> data, int fieldIndex)
        => FlatBufferTableObject.Read(this, fieldIndex, data);
    public FlatBufferTableString ReadAsStringTable(ReadOnlySpan<byte> data, int fieldIndex)
        => FlatBufferTableString.Read(this, fieldIndex, data);

    public FlatBufferObject ReadAsObject(ReadOnlySpan<byte> data, int fieldIndex)
        => FlatBufferObject.Read(this, fieldIndex, data);

    public FlatBufferStringValue ReadAsString(ReadOnlySpan<byte> data, int fieldIndex)
        => FlatBufferStringValue.Read(this, fieldIndex, data);

    public FlatBufferFieldValue<T> ReadAs<T>(ReadOnlySpan<byte> data, int fieldIndex)
        where T : struct
        => FlatBufferFieldValue<T>.Read(this, fieldIndex, data, Type.GetTypeCode(typeof(T)));

    public FlatBufferNode ReadAs(ReadOnlySpan<byte> data, int fieldIndex, TypeCode type) => type switch
    {
#pragma warning disable format
        TypeCode.Boolean => ReadAs<bool  >(data, fieldIndex),

        TypeCode.SByte   => ReadAs<sbyte >(data, fieldIndex),
        TypeCode.Int16   => ReadAs<short >(data, fieldIndex),
        TypeCode.Int32   => ReadAs<int   >(data, fieldIndex),
        TypeCode.Int64   => ReadAs<long  >(data, fieldIndex),

        TypeCode.Byte    => ReadAs<byte  >(data, fieldIndex),
        TypeCode.UInt16  => ReadAs<ushort>(data, fieldIndex),
        TypeCode.UInt32  => ReadAs<uint  >(data, fieldIndex),
        TypeCode.UInt64  => ReadAs<ulong >(data, fieldIndex),

        TypeCode.Single  => ReadAs<float >(data, fieldIndex),
        TypeCode.Double  => ReadAs<double>(data, fieldIndex),

        TypeCode.String  => ReadAsString  (data, fieldIndex),
        TypeCode.Object  => ReadAsObject  (data, fieldIndex),
#pragma warning restore format
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public FlatBufferTableStruct<T> ReadArrayAs<T>(ReadOnlySpan<byte> data, int fieldIndex)
        where T : struct
        => FlatBufferTableStruct<T>.Read(this, fieldIndex, data, Type.GetTypeCode(typeof(T)));

    public FlatBufferNode ReadArrayAs(ReadOnlySpan<byte> data, int fieldIndex, TypeCode type) => type switch
    {
#pragma warning disable format
        TypeCode.Boolean => ReadArrayAs<bool  >(data, fieldIndex),

        TypeCode.SByte   => ReadArrayAs<sbyte >(data, fieldIndex),
        TypeCode.Int16   => ReadArrayAs<short >(data, fieldIndex),
        TypeCode.Int32   => ReadArrayAs<int   >(data, fieldIndex),
        TypeCode.Int64   => ReadArrayAs<long  >(data, fieldIndex),

        TypeCode.Byte    => ReadArrayAs<byte  >(data, fieldIndex),
        TypeCode.UInt16  => ReadArrayAs<ushort>(data, fieldIndex),
        TypeCode.UInt32  => ReadArrayAs<uint  >(data, fieldIndex),
        TypeCode.UInt64  => ReadArrayAs<ulong >(data, fieldIndex),

        TypeCode.Single  => ReadArrayAs<float >(data, fieldIndex),
        TypeCode.Double  => ReadArrayAs<double>(data, fieldIndex),

        TypeCode.String  => ReadAsStringTable  (data, fieldIndex),
        TypeCode.Object  => ReadAsTable        (data, fieldIndex),
#pragma warning restore format
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
