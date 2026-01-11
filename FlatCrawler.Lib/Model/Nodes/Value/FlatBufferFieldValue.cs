using System;
using System.Runtime.InteropServices;

namespace FlatCrawler.Lib;

/// <summary>
/// Node comprised of a serialized struct value.
/// </summary>
/// <typeparam name="T">integral numeric types and floating point types only.</typeparam>
public sealed record FlatBufferFieldValue<T> : FlatBufferNode, IStructNode where T : struct
{
    /// <summary> The type of the value, <typeparamref name="T"/>. </summary>
    public TypeCode Type { get; }

    /// <summary> The value of the node. </summary>
    public T Value { get; }

    public override string TypeName
    {
        get => Value switch
        {
            bool b => $"{Type.ToTypeString()} {b}",
            IFormattable f => GetFormattedValue(f, Type),
            _ => base.TypeName,
        };
        set { }
    }

    private static string GetFormattedValue(IFormattable f, TypeCode type) => type switch
    {
        TypeCode.Single or TypeCode.Double => $"{type.ToTypeString()} {f:R}",
        TypeCode.Byte   or TypeCode.SByte  => $"{type.ToTypeString()} {f:X2} ({f})",
        TypeCode.UInt16 or TypeCode.Int16  => $"{type.ToTypeString()} {f:X4} ({f})",
        TypeCode.UInt32 or TypeCode.Int32  => $"{type.ToTypeString()} {f:X8} ({f})",
        TypeCode.UInt64 or TypeCode.Int64  => $"{type.ToTypeString()} {f:X16} ({f})",
        _ => $"{type.ToTypeString()} {f:X} ({f})",
    };

    private FlatBufferFieldValue(T value, TypeCode type, FlatBufferNode parent, int offset) : base(offset, parent)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// Reads a new node of the specified type from the provided inputs.
    /// </summary>
    /// <param name="offset">Offset of the new node</param>
    /// <param name="parent">Parent that owns this child node</param>
    /// <param name="data">FlatBuffer binary</param>
    /// <param name="type">Type of the new node</param>
    /// <returns>A new node of the specified type</returns>
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

    /// <summary>
    /// Reads a new node of the specified type from the provided inputs.
    /// </summary>
    /// <param name="parent">Parent that owns this child node</param>
    /// <param name="fieldIndex">Index of the field in the parent's VTable</param>
    /// <param name="data">FlatBuffer binary</param>
    /// <param name="type">Type of the new node</param>
    /// <returns>A new node of the specified type</returns>
    public static FlatBufferFieldValue<T> Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data, TypeCode type) => Read(parent.GetFieldOffset(fieldIndex), parent, data, type);
}
