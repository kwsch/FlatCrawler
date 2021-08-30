using System;
using System.Runtime.InteropServices;

namespace FlatCrawler.Lib
{
    public sealed record FlatBufferFieldValue<T> : FlatBufferNode where T : struct
    {
        public override string Name => $"{Type}";
        public TypeCode Type { get; }
        public T Value { get; }

        private FlatBufferFieldValue(T value, TypeCode type, FlatBufferNode parent, int offset) : base(offset, parent)
        {
            Type = type;
            Value = value;
        }

        public static FlatBufferFieldValue<T> Read(int offset, FlatBufferNode parent, byte[] data, TypeCode type)
        {
            var value = MemoryMarshal.Cast<byte, T>(data.AsSpan(offset))[0];
            return new FlatBufferFieldValue<T>(value, type, parent, offset);
        }

        public static FlatBufferFieldValue<T> Read(FlatBufferNodeField parent, int fieldIndex, byte[] data, TypeCode type) => Read(parent.GetFieldOffset(fieldIndex), parent, data, type);
    }
}
