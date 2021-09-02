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

        public string GetFieldOrder()
        {
            var tuples = FieldOffsets.Where(z => z.Offset != 0).OrderBy(z => z.Offset);
            return string.Join(" ", GetFieldPrint(tuples));
        }

        public override string ToString() => $@"VTable @ 0x{Location:X}
VTable Size: {VTableLength}
Table Size: {TableLength}
Fields: {FieldOffsets.Length}: {string.Join(" ", GetFieldPrint(FieldOffsets))}";

        private const int fieldsPerLine = 8;

        private static IEnumerable<string?> GetFieldPrint(IEnumerable<VTableFieldInfo> fields)
        {
            static string PrintField(VTableFieldInfo z, int printedIndex) => $"{(printedIndex % fieldsPerLine == 0 ? Environment.NewLine : "")}{z.Index:00}: {z.Offset:X4}  ";
            return fields.Select(PrintField);
        }
    }
}
