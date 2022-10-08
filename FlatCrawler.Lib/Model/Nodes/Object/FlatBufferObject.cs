﻿using System;

namespace FlatCrawler.Lib
{
    public sealed record FlatBufferObject : FlatBufferNodeField
    {
        public override string TypeName { get; set; } = "Object";

        private FlatBufferObject(int offset, VTable vTable, int dataTableOffset, int vTableOffset, FlatBufferNode parent) :
            base(offset, vTable, dataTableOffset, vTableOffset, parent)
        {
            TypeName = $"Object [{vTable.FieldInfo.Length}]";
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
            return new FlatBufferObject(offset, vTable, tableOffset, vTableOffset, parent);
        }

        public static FlatBufferObject Read(FlatBufferNodeField parent, int fieldIndex, byte[] data)
        {
            var offset = parent.GetReferenceOffset(fieldIndex, data);
            return Read(offset, parent, data);
        }
    }
}
