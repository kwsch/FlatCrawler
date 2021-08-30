using System;

namespace FlatCrawler.Lib
{
    public sealed record FlatBufferTableStruct<T> : FlatBufferTable<FlatBufferFieldValue<T>> where T : struct
    {
        public override string Name => $"{ArrayType}[]";
        public TypeCode ArrayType { get; }

        private FlatBufferTableStruct(int offset, int length, FlatBufferNode parent, int dataTableOffset, TypeCode typeCode) : base(offset, parent, length, dataTableOffset)
        {
            ArrayType = typeCode;
        }

        private void ReadArray(byte[] data)
        {
            for (int i = 0; i < Entries.Length; i++)
                Entries[i] = GetEntryAtIndex(data, i);
        }

        public override FlatBufferNode GetEntry(int entryIndex) => Entries[entryIndex];

        private FlatBufferFieldValue<T> GetEntryAtIndex(byte[] data, int entryIndex)
        {
            var offset = DataTableOffset + (entryIndex * 4);
            return FlatBufferFieldValue<T>.Read(offset, this, data, ArrayType);
        }

        public static FlatBufferTableStruct<T> Read(int offset, FlatBufferNode parent, byte[] data, TypeCode type)
        {
            int length = BitConverter.ToInt32(data, offset);
            var node = new FlatBufferTableStruct<T>(offset, length, parent, offset + 4, type);
            node.ReadArray(data);
            return node;
        }

        public static FlatBufferTableStruct<T> Read(FlatBufferNodeField parent, int fieldIndex, byte[] data, TypeCode type) => Read(parent.GetFieldOffset(fieldIndex), parent, data, type);
    }
}
