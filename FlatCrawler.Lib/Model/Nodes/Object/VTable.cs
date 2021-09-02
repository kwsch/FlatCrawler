using System;
using System.Collections.Generic;
using System.Linq;

namespace FlatCrawler.Lib
{
    public sealed class VTable
    {
        private readonly int Location;
        public readonly short VTableLength;
        private readonly short TableLength;
        public readonly VTableFieldInfo[] FieldOffsets;

        public VTable(byte[] data, int offset)
        {
            Location = offset;
            VTableLength = BitConverter.ToInt16(data, offset);
            TableLength = BitConverter.ToInt16(data, offset + 2);
            var fieldCount = (VTableLength - 4) / 2;
            FieldOffsets = ReadFieldOffsets(data, offset + 4, fieldCount);

            if (FieldOffsets.Any(z => z.Offset >= TableLength))
                throw new IndexOutOfRangeException("Field offset is beyond the data table's length.");
        }

        private static VTableFieldInfo[] ReadFieldOffsets(byte[] data, int offset, int fieldCount)
        {
            var result = new VTableFieldInfo[fieldCount];
            for (int i = 0; i < fieldCount; i++)
            {
                var ofs = BitConverter.ToInt16(data, offset + (i * 2));
                result[i] = new VTableFieldInfo(i, ofs);
            }
            return result;
        }

        public int GetFieldIndex(int offset)
        {
            if (offset == 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Value of 0 for offset is not valid (Default Value)");

            var index = Array.FindIndex(FieldOffsets, z => z.Offset == offset);
            if (index == -1)
                throw new ArgumentOutOfRangeException(nameof(offset), "Unable to find the field index with that offset.");

            return index;
        }

        public string GetFieldOrder(int bias = 0)
        {
            var tuples = FieldOffsets.Where(z => z.Offset != 0).OrderBy(z => z.Offset);
            return string.Join(" ", GetFieldPrint(tuples, bias));
        }

        public override string ToString() => $@"VTable @ 0x{Location:X}
VTable Size: {VTableLength}
Table Size: {TableLength}
Fields: {FieldOffsets.Length}: {string.Join(" ", GetFieldPrint(FieldOffsets))}";

        private const int fieldsPerLine = 8;

        private static IEnumerable<string?> GetFieldPrint(IEnumerable<VTableFieldInfo> fields, int bias = 0)
        {
            string PrintField(VTableFieldInfo z, int printedIndex) => $"{(printedIndex % fieldsPerLine == 0 ? Environment.NewLine : "")}{z.Index:00}: {z.Offset + bias:X4}  ";
            return fields.Select(PrintField);
        }
    }
}
