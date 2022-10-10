using System;
using System.Collections.Generic;
using System.Linq;

namespace FlatCrawler.Lib;

public sealed class VTable
{
    public readonly int Location;
    public readonly short VTableLength;
    public readonly short DataTableLength;
    public readonly VTableFieldInfo[] FieldInfo;

    private const int SizeOfVTableLength = sizeof(ushort);
    private const int SizeOfDataTableLength = sizeof(ushort);
    private const int SizeOfField = sizeof(ushort);
    public const int HeaderSize = SizeOfVTableLength + SizeOfDataTableLength;

    public VTable(byte[] data, int offset)
    {
        Location = offset;
        VTableLength = BitConverter.ToInt16(data, offset);
        DataTableLength = BitConverter.ToInt16(data, offset + SizeOfVTableLength);
        var fieldCount = (VTableLength - HeaderSize) / SizeOfField;

        FieldInfo = new VTableFieldInfo[fieldCount];
        ReadFieldInfo(data, offset + HeaderSize);

        if (FieldInfo.Any(z => z.HasValue && z.Offset >= DataTableLength))
            throw new IndexOutOfRangeException("Field offset is beyond the data table's length.");
    }

    private void ReadFieldInfo(byte[] data, int offset)
    {
        int fieldCount = FieldInfo.Length;

        // Store index and offset
        (int Index, int Offset)[] fields = new (int, int)[fieldCount];
        for (int i = 0; i < fieldCount; i++)
        {
            var ofs = BitConverter.ToInt16(data, offset + (i * SizeOfField));
            fields[i] = new(i, ofs);
        }

        var sortedFields = fields.OrderBy(z => z.Offset).ToArray();

        // Loop in reverse order, starting at the table size
        // Field size would be Start byte - End byte.
        // Eg. 12 (table length) - 8 (offset) = size of 4 bytes
        // Next field would end at 8

        int end = DataTableLength;
        for (int i = fieldCount - 1; i >= 0; --i)
        {
            var index = sortedFields[i].Index;
            var start = sortedFields[i].Offset;
            FieldInfo[index] = new(index, start, end - start);
            end = start;
        }
    }

    public int GetFieldIndex(int offset)
    {
        if (offset == 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Value of 0 for offset is not valid (Default Value)");

        var index = Array.FindIndex(FieldInfo, z => z.Offset == offset);
        if (index == -1)
            throw new ArgumentOutOfRangeException(nameof(offset), "Unable to find the field index with that offset.");

        return index;
    }

    public string GetFieldOrder(int bias = 0)
    {
        var tuples = FieldInfo.Where(z => z.HasValue).OrderBy(z => z.Offset);
        return string.Join(" ", GetFieldPrint(tuples, bias));
    }

    public override string ToString() => $@"VTable @ 0x{Location:X}
VTable Size: {VTableLength}
DataTable Size: {DataTableLength}
Fields: {FieldInfo.Length}: {string.Join(" ", GetFieldPrint(FieldInfo))}";

    private const int fieldsPerLine = 8;

    private static IEnumerable<string?> GetFieldPrint(IEnumerable<VTableFieldInfo> fields, int bias = 0)
    {
        string PrintField(VTableFieldInfo z, int printedIndex) => $"{(printedIndex % fieldsPerLine == 0 ? Environment.NewLine : "")}{z.Index:00}: {z.Offset + bias:X4}  ";
        return fields.Select(PrintField);
    }
}
