using System;

namespace FlatCrawler.Lib
{
    public sealed record FlatBufferTableObject : FlatBufferTable<FlatBufferObject>
    {
        private string _name = "???";
        private string _typeName = "Object[]";
        public override string TypeName
        {
            get => _typeName;
            set
            {
                _typeName = value + "[]";
                foreach (var e in Entries)
                {
                    e.TypeName = value;
                }
            }
        }

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                for (int i = 0; i < Entries.Length; ++i)
                {
                    Entries[i].Name = $"{value}[{i}]";
                }
            }
        }

        public override FlatBufferObject GetEntry(int entryIndex) => Entries[entryIndex];

        private FlatBufferTableObject(int offset, int length, FlatBufferNode parent, int dataTableOffset) :
            base(offset, parent, length, dataTableOffset)
        { }

        private void ReadArray(byte[] data)
        {
            for (int i = 0; i < Entries.Length; i++)
                Entries[i] = GetEntryAtIndex(data, i);
        }

        private FlatBufferObject GetEntryAtIndex(byte[] data, int entryIndex)
        {
            var arrayEntryPointerOffset = DataTableOffset + (entryIndex * 4);
            var dataTablePointerShift = BitConverter.ToInt32(data, arrayEntryPointerOffset);
            var dataTableOffset = arrayEntryPointerOffset + dataTablePointerShift;

            return FlatBufferObject.Read(arrayEntryPointerOffset, this, data, dataTableOffset);
        }

        public static FlatBufferTableObject Read(int offset, FlatBufferNode parent, byte[] data)
        {
            int length = BitConverter.ToInt32(data, offset);
            var node = new FlatBufferTableObject(offset, length, parent, offset + 4);
            node.ReadArray(data);
            return node;
        }

        public static FlatBufferTableObject Read(FlatBufferNodeField parent, int fieldIndex, byte[] data) => Read(parent.GetReferenceOffset(fieldIndex, data), parent, data);
    }
}
