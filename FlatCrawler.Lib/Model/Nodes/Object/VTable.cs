using System;
using System.Collections.Generic;
using System.Linq;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

/// <summary>
/// Virtual method table containing pointers to the schema object's field data.
/// Indicates if a field is present with data for the serialized schema object.
/// </summary>
public sealed class VTable
{
    /// <summary>
    /// Absolute offset of the VTable in the data.
    /// </summary>
    public readonly int Location;

    /// <summary>
    /// The size of the VTable in bytes.
    /// </summary>
    public readonly short VTableLength;

    /// <summary>
    /// The size of the object's serialized data in bytes.
    /// </summary>
    public readonly short DataTableLength;

    /// <summary>
    /// A list of fields that may be present in the serialized object.
    /// </summary>
    public readonly VTableFieldInfo[] FieldInfo;

    private const int SizeOfVTableLength = sizeof(ushort);
    private const int SizeOfDataTableLength = sizeof(ushort);
    private const int SizeOfField = sizeof(ushort);
    private const int HeaderSize = SizeOfVTableLength + SizeOfDataTableLength;

    public int RefCount { get; set; } = 0;

    public DataRange VTableMemory => new(Location..(Location + VTableLength), DataCategory.VTable, () => "VTable");

    public VTable(FlatBufferFile file, int offset)
    {
        Location = offset;
        var data = file.Data[offset..]; // adjust view window to be relative to vtable location
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

        file.EnsureNoAccessViolation(VTableMemory);

        DataTableLength = ReadInt16LittleEndian(data[SizeOfVTableLength..]);
        var fieldCount = (VTableLength - HeaderSize) / SizeOfField;

        FieldInfo = ReadFieldInfo(data[HeaderSize..], fieldCount);
    }

    private VTableFieldInfo[] ReadFieldInfo(ReadOnlySpan<byte> data, int fieldCount)
    {
        var result = new VTableFieldInfo[fieldCount];

        // Store index and offset
        short[] offsets = new short[fieldCount];
        for (int i = 0; i < fieldCount; i++)
        {
            var ofs = ReadInt16LittleEndian(data[(i * SizeOfField)..]);
            offsets[i] = ofs;

            var z = new VTableFieldInfo(i, ofs, 0);
            if (z.HasValue && z.Offset >= DataTableLength)
                throw new IndexOutOfRangeException("Field offset is beyond the data table's length.");
            result[i] = z;
        }

        UpdateSizes(result, result, DataTableLength);

        return result;
    }

    private static void UpdateSizes(VTableFieldInfo[] ascendingIndex, VTableFieldInfo[] result, int end)
    {
        // Loop in reverse order, starting at the table size
        // Field size would be Start byte - End byte.
        // Eg. 12 (table length) - 8 (offset) = size of 4 bytes
        // Next field would end at 8

        // Store index and offset in reverse order
        var sortedDescendingOffsets = ascendingIndex.GetOrderedList();
        foreach ((int offset, int index) in sortedDescendingOffsets)
        {
            ref var exist = ref result[index];
            var size = end - offset;
            exist = exist with { Size = size };
            end = offset;
        }
    }

    /// <summary>
    /// Gets the index of the field within <see cref="FieldInfo"/> that exists at the requested offset.
    /// </summary>
    /// <param name="offset">Relative offset (raw value in VTable).</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int GetFieldIndex(int offset)
    {
        if (offset == 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Value of 0 for offset is not valid (Default Value)");

        var index = Array.FindIndex(FieldInfo, z => z.Offset == offset);
        if (index == -1)
            throw new ArgumentOutOfRangeException(nameof(offset), "Unable to find the field index with that offset.");

        return index;
    }

    /// <summary>
    /// Gets a printable string of the VTable's data.
    /// </summary>
    /// <param name="bias">Offset shift to add to each field's relative offset pointer. Useful to point to the absolute offset for manual analysis.</param>
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
