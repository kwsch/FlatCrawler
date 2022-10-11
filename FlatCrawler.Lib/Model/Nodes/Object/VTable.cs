using System;
using System.Collections.Generic;
using System.Linq;
using static System.Buffers.Binary.BinaryPrimitives;

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

    public VTable(ReadOnlySpan<byte> data, int offset)
    {
        Location = offset;
        data = data[offset..]; // adjust view window to be relative to vtable location
        VTableLength = ReadInt16LittleEndian(data);

        // Validate VTable (from https://github.com/dvidelabs/flatcc/blob/master/doc/binary-format.md#verification)
        // > vtable size is at least the two header fields (2 * sizeof(voffset_t)`).
        if (VTableLength < HeaderSize)
            throw new AccessViolationException("Tried to create a VTable from invalid data.");

        // > vtable size is aligned and does not end outside buffer.
        if (VTableLength > data.Length)
            throw new AccessViolationException("VTable is beyond the file data length.");

        if (!MemoryUtil.IsAligned((uint)(Location + VTableLength), SizeOfField))
            throw new AccessViolationException("Invalid VTable, VTable is not aligned.");

        DataTableLength = ReadInt16LittleEndian(data[SizeOfVTableLength..]);
        var fieldCount = (VTableLength - HeaderSize) / SizeOfField;

        FieldInfo = new VTableFieldInfo[fieldCount];
        ReadFieldInfo(data[HeaderSize..]);

        if (FieldInfo.Any(z => z.HasValue && z.Offset >= DataTableLength))
            throw new IndexOutOfRangeException("Field offset is beyond the data table's length.");
    }

    private void ReadFieldInfo(ReadOnlySpan<byte> data)
    {
        int fieldCount = FieldInfo.Length;

        // Store index and offset
        short[] offsets = new short[fieldCount];
        for (int i = 0; i < fieldCount; i++)
        {
            var ofs = ReadInt16LittleEndian(data[(i * SizeOfField)..]);
            offsets[i] = ofs;
            FieldInfo[i] = new(i, ofs, 0);
        }

        // Loop in reverse order, starting at the table size
        // Field size would be Start byte - End byte.
        // Eg. 12 (table length) - 8 (offset) = size of 4 bytes
        // Next field would end at 8

        // Store index and offset in reverse order
        var sortedFields = offsets
            .Select((Offset, Index) => new { Offset, Index })
            .Where(x => x.Offset != 0) // Zero offsets don't exist in the table so have no size
            .OrderByDescending(z => z.Offset)
            .ToArray();

        int end = DataTableLength;
        foreach (var f in sortedFields)
        {
            var i = f.Index;
            var start = f.Offset;
            FieldInfo[i] = new(i, start, end - start);
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
