using System;
using System.Linq;

namespace FlatCrawler.Lib
{
    public sealed class VTable
    {
        private readonly int Location;
        private readonly short VTableLength;
        private readonly short TableLength;
        public readonly VTableFieldInfo[] FieldOffsets;

        public VTable(byte[] data, int offset)
        {
            Location = offset;
            VTableLength = BitConverter.ToInt16(data, offset);
            TableLength = BitConverter.ToInt16(data, offset + 2);
            var fieldCount = (VTableLength - 4) / 2;
            FieldOffsets = ReadFieldOffsets(data, offset + 4, fieldCount);
        }

        private static VTableFieldInfo[] ReadFieldOffsets(byte[] data, int offset, int fieldCount)
        {
            var result = new VTableFieldInfo[fieldCount];
            for (int i = 0; i < fieldCount; i++)
            {
                var ofs = BitConverter.ToInt16(data, offset + (i * 2));
                result[i] = new VTableFieldInfo(ofs);
            }
            return result;
        }

        public override string ToString() => $@"VTable @ 0x{Location:X}
VTable Size: {VTableLength}
Table Size: {TableLength}
Fields: {FieldOffsets.Length} - {string.Join(" ", FieldOffsets.Select(z => z.ToString()))}";
    }
}
