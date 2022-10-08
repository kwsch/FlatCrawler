using System;
using System.Collections.Generic;

namespace FlatCrawler.Lib;

public abstract record FlatBufferNodeField : FlatBufferNode, IFieldNode
{
    public VTable VTable { get; }
    public int DataTableOffset { get; }
    public int VTableOffset { get; }

    private FlatBufferNode?[] Fields { get; }
    public IReadOnlyList<FlatBufferNode?> AllFields => Fields;

    public bool HasField(int fieldIndex) => fieldIndex < VTable.FieldInfo.Length && VTable.FieldInfo[fieldIndex].Offset != 0;
    public int FieldCount => Fields.Length;

    protected FlatBufferNodeField(int offset, VTable vTable, int dataTableOffset, int vTableOffset, FlatBufferNode? parent = null) :
        base(offset, parent)
    {
        VTable = vTable;
        DataTableOffset = dataTableOffset;
        VTableOffset = vTableOffset;
        Fields = new FlatBufferNode[vTable.FieldInfo.Length];
    }

    public int GetFieldOffset(int fieldIndex)
    {
        var fo = VTable.FieldInfo[fieldIndex];
        if (fo.Offset == 0)
            throw new ArgumentException("Field not present in Table");
        return DataTableOffset + fo.Offset;
    }

    public int GetReferenceOffset(int fieldIndex, byte[] data)
    {
        var fieldOffset = GetFieldOffset(fieldIndex);
        var rawPtr = BitConverter.ToInt32(data, fieldOffset);
        return fieldOffset + rawPtr;
    }

    public override int GetChildIndex(FlatBufferNode? child)
    {
        if (child is null)
            return -1;
        return Array.FindIndex(Fields, z => ReferenceEquals(z, child));
    }

    protected static int GetVtableOffset(int offset, byte[] data, bool reverse = false)
    {
        var ofs = BitConverter.ToInt32(data, offset);
        return reverse ? offset - ofs : offset + ofs;
    }

    protected static VTable ReadVTable(int offset, byte[] data) => new(data, offset);

    public void TrackChildFieldNode(int fieldIndex, TypeCode code, bool asArray, FlatBufferNode node)
    {
        // Table objects have the same data types for each entry
        if (Parent is FlatBufferTableObject t)
        {
            t.OnFieldTypeChanged(fieldIndex, code, asArray, this);
        }

        Fields[fieldIndex] = node;
    }

    public FlatBufferNode GetFieldValue(int fieldIndex, byte[] data, TypeCode type) => type switch
    {
#pragma warning disable format
        TypeCode.Boolean => ReadBool   (fieldIndex, data),

        TypeCode.SByte   => ReadInt8   (fieldIndex, data),
        TypeCode.Int16   => ReadInt16  (fieldIndex, data),
        TypeCode.Int32   => ReadInt32  (fieldIndex, data),
        TypeCode.Int64   => ReadInt64  (fieldIndex, data),

        TypeCode.Byte    => ReadUInt8  (fieldIndex, data),
        TypeCode.UInt16  => ReadUInt16 (fieldIndex, data),
        TypeCode.UInt32  => ReadUInt32 (fieldIndex, data),
        TypeCode.UInt64  => ReadUInt64 (fieldIndex, data),

        TypeCode.Single  => ReadFloat  (fieldIndex, data),
        TypeCode.Double  => ReadDouble (fieldIndex, data),

        TypeCode.String  => ReadString (fieldIndex, data),
        TypeCode.Object  => ReadObject (fieldIndex, data),
#pragma warning restore format
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public FlatBufferNode GetTableStruct(int fieldIndex, byte[] data, TypeCode type) => type switch
    {
#pragma warning disable format
        TypeCode.Boolean =>  ReadArrayBool   (fieldIndex, data),

        TypeCode.SByte   =>  ReadArrayInt8   (fieldIndex, data),
        TypeCode.Int16   =>  ReadArrayInt16  (fieldIndex, data),
        TypeCode.Int32   =>  ReadArrayInt32  (fieldIndex, data),
        TypeCode.Int64   =>  ReadArrayInt64  (fieldIndex, data),

        TypeCode.Byte    =>  ReadArrayUInt8  (fieldIndex, data),
        TypeCode.UInt16  =>  ReadArrayUInt16 (fieldIndex, data),
        TypeCode.UInt32  =>  ReadArrayUInt32 (fieldIndex, data),
        TypeCode.UInt64  =>  ReadArrayUInt64 (fieldIndex, data),

        TypeCode.Single  =>  ReadArrayFloat  (fieldIndex, data),
        TypeCode.Double  =>  ReadArrayDouble (fieldIndex, data),

        TypeCode.String  =>  ReadArrayString (fieldIndex, data),
        TypeCode.Object  =>  ReadArrayObject (fieldIndex, data),
#pragma warning restore format
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public void UpdateNodeType(int fieldIndex, byte[] data, TypeCode type, bool asArray)
    {
        Fields[fieldIndex] = ReadNode(fieldIndex, data, type, asArray);
    }

    public FlatBufferNode ReadNode(int fieldIndex, byte[] data, TypeCode type, bool asArray)
    {
        if (asArray)
            return GetTableStruct(fieldIndex, data, type);
        return GetFieldValue(fieldIndex, data, type);
    }

#pragma warning disable format
    public FlatBufferObject      ReadObject     (int fieldIndex, byte[] data) => FlatBufferObject     .Read(this, fieldIndex, data);
    public FlatBufferStringValue ReadString     (int fieldIndex, byte[] data) => FlatBufferStringValue.Read(this, fieldIndex, data);
    public FlatBufferTableObject ReadArrayObject(int fieldIndex, byte[] data) => FlatBufferTableObject.Read(this, fieldIndex, data);
    public FlatBufferTableString ReadArrayString(int fieldIndex, byte[] data) => FlatBufferTableString.Read(this, fieldIndex, data);

    public FlatBufferFieldValue<bool  > ReadBool   (int fieldIndex, byte[] data) => FlatBufferFieldValue<bool  >.Read(this, fieldIndex, data, TypeCode.Boolean);

    public FlatBufferFieldValue<sbyte > ReadInt8   (int fieldIndex, byte[] data) => FlatBufferFieldValue<sbyte >.Read(this, fieldIndex, data, TypeCode.SByte  );
    public FlatBufferFieldValue<short > ReadInt16  (int fieldIndex, byte[] data) => FlatBufferFieldValue<short >.Read(this, fieldIndex, data, TypeCode.Int16  );
    public FlatBufferFieldValue<int   > ReadInt32  (int fieldIndex, byte[] data) => FlatBufferFieldValue<int   >.Read(this, fieldIndex, data, TypeCode.Int32  );
    public FlatBufferFieldValue<long  > ReadInt64  (int fieldIndex, byte[] data) => FlatBufferFieldValue<long  >.Read(this, fieldIndex, data, TypeCode.Int64  );

    public FlatBufferFieldValue<byte  > ReadUInt8  (int fieldIndex, byte[] data) => FlatBufferFieldValue<byte  >.Read(this, fieldIndex, data, TypeCode.Byte   );
    public FlatBufferFieldValue<ushort> ReadUInt16 (int fieldIndex, byte[] data) => FlatBufferFieldValue<ushort>.Read(this, fieldIndex, data, TypeCode.UInt16 );
    public FlatBufferFieldValue<uint  > ReadUInt32 (int fieldIndex, byte[] data) => FlatBufferFieldValue<uint  >.Read(this, fieldIndex, data, TypeCode.UInt32 );
    public FlatBufferFieldValue<ulong > ReadUInt64 (int fieldIndex, byte[] data) => FlatBufferFieldValue<ulong >.Read(this, fieldIndex, data, TypeCode.UInt64 );

    public FlatBufferFieldValue<float > ReadFloat  (int fieldIndex, byte[] data) => FlatBufferFieldValue<float >.Read(this, fieldIndex, data, TypeCode.Single );
    public FlatBufferFieldValue<double> ReadDouble (int fieldIndex, byte[] data) => FlatBufferFieldValue<double>.Read(this, fieldIndex, data, TypeCode.Double );

    public FlatBufferTableStruct<bool  > ReadArrayBool   (int fieldIndex, byte[] data) => FlatBufferTableStruct<bool  >.Read(this, fieldIndex, data, TypeCode.Boolean);

    public FlatBufferTableStruct<sbyte > ReadArrayInt8   (int fieldIndex, byte[] data) => FlatBufferTableStruct<sbyte >.Read(this, fieldIndex, data, TypeCode.SByte  );
    public FlatBufferTableStruct<short > ReadArrayInt16  (int fieldIndex, byte[] data) => FlatBufferTableStruct<short >.Read(this, fieldIndex, data, TypeCode.Int16  );
    public FlatBufferTableStruct<int   > ReadArrayInt32  (int fieldIndex, byte[] data) => FlatBufferTableStruct<int   >.Read(this, fieldIndex, data, TypeCode.Int32  );
    public FlatBufferTableStruct<long  > ReadArrayInt64  (int fieldIndex, byte[] data) => FlatBufferTableStruct<long  >.Read(this, fieldIndex, data, TypeCode.Int64  );

    public FlatBufferTableStruct<byte  > ReadArrayUInt8  (int fieldIndex, byte[] data) => FlatBufferTableStruct<byte  >.Read(this, fieldIndex, data, TypeCode.Byte   );
    public FlatBufferTableStruct<ushort> ReadArrayUInt16 (int fieldIndex, byte[] data) => FlatBufferTableStruct<ushort>.Read(this, fieldIndex, data, TypeCode.UInt16 );
    public FlatBufferTableStruct<uint  > ReadArrayUInt32 (int fieldIndex, byte[] data) => FlatBufferTableStruct<uint  >.Read(this, fieldIndex, data, TypeCode.UInt32 );
    public FlatBufferTableStruct<ulong > ReadArrayUInt64 (int fieldIndex, byte[] data) => FlatBufferTableStruct<ulong >.Read(this, fieldIndex, data, TypeCode.UInt64 );

    public FlatBufferTableStruct<float > ReadArrayFloat  (int fieldIndex, byte[] data) => FlatBufferTableStruct<float >.Read(this, fieldIndex, data, TypeCode.Single );
    public FlatBufferTableStruct<double> ReadArrayDouble (int fieldIndex, byte[] data) => FlatBufferTableStruct<double>.Read(this, fieldIndex, data, TypeCode.Double );
#pragma warning restore format
}
