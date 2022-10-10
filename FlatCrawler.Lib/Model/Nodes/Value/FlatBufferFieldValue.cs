using System;
using System.Runtime.InteropServices;

namespace FlatCrawler.Lib;

public sealed record FlatBufferFieldValue<T> : FlatBufferNode, IStructNode where T : struct
{
    public override string TypeName { get => Value is not IFormattable f ? Type.ToString() : Type is not TypeCode.Single or TypeCode.Double ? $"{Type} {f:X} ({f})" : $"{Type} {f}"; set { } }
    public TypeCode Type { get; }
    public T Value { get; }

    private FlatBufferFieldValue(T value, TypeCode type, FlatBufferNode parent, int offset) : base(offset, parent)
    {
        Type = type;
        Value = value;
    }

    public static FlatBufferFieldValue<T> Read(int offset, FlatBufferNode parent, ReadOnlySpan<byte> data, TypeCode type)
    {
        if (!BitConverter.IsLittleEndian)
        {
            Span<byte> temp = stackalloc byte[Marshal.SizeOf<T>()];
            data.Slice(offset, temp.Length).CopyTo(temp);
            temp.Reverse();
            var value = MemoryMarshal.Read<T>(temp);
            return new FlatBufferFieldValue<T>(value, type, parent, offset);
        }
        else
        {
            var value = MemoryMarshal.Read<T>(data[offset..]);
            return new FlatBufferFieldValue<T>(value, type, parent, offset);
        }
    }

    public static FlatBufferFieldValue<T> Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data, TypeCode type) => Read(parent.GetFieldOffset(fieldIndex), parent, data, type);
}
