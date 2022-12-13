using System;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

/// <summary>
/// Node that contains a serialized schema object.
/// </summary>
/// <remarks>
/// Very similar to <see cref="FlatBufferObject"/>, except it can be prefixed by a magic file identifier.
/// Usually is a unique schema to serve as an entry point for end users.
/// </remarks>
public sealed record FlatBufferRoot : FlatBufferObject
{
    private const int MaxMagicLength = 4;

    public override FlatBufferFile FbFile { get; }
    public string? Magic { get; }

    public override string TypeName { get => "Root"; set { } }
    private DataRange NodeMemory => new(..Size, DataCategory.Misc, () => TypeName);
    private int MagicLength => Magic is null ? 0 : MaxMagicLength;

    private FlatBufferRoot(FlatBufferFile file, VTable vTable, string? magic, int dataTableOffset) :
        base(file, vTable, 0, dataTableOffset, null)
    {
        FbFile = file;
        Magic = magic;
        FieldInfo = FieldInfo with { Size = (HeaderSize + MagicLength) };
        RegisterMemory();
    }

    public override void RegisterMemory()
    {
        FbFile.SetProtectedMemory(NodeMemory);
        base.RegisterMemory();
    }

    public static FlatBufferRoot Read(FlatBufferFile file, int offset)
    {
        var data = file.Data;
        int dataTableOffset = ReadInt32LittleEndian(data[offset..]) + offset;
        var vTableOffset = GetVtableOffset(dataTableOffset, data, true);

        // Check if there's 4 bytes between the Data Table Pointer and the start of the VTable.
        // If there's 4 bytes, we might have a Header Magic present.
        var bytesAvailableForHeader = vTableOffset - HeaderSize - offset;
        var magic = ((bytesAvailableForHeader < MaxMagicLength) ? null : ReadMagic(offset + HeaderSize, data));

        // Read VTable
        var vTable = file.PeekVTable(vTableOffset);
        file.RegisterVTable(vTable);
        return new FlatBufferRoot(file, vTable, magic, dataTableOffset);
    }

    private static string? ReadMagic(int offset, ReadOnlySpan<byte> data)
    {
        // Identifiers must always be exactly 4 characters long.
        // These 4 characters will end up as bytes at offsets 4-7 (inclusive) in the buffer.
        var magicRegion = data.Slice(offset, MaxMagicLength);
        var count = GetMagicCharCount(magicRegion);
        if (count != 4)
            return null;
        return Encoding.ASCII.GetString(magicRegion[..count]);
    }

    private static int GetMagicCharCount(ReadOnlySpan<byte> data)
    {
        // Return the count of ASCII readable byte chars in data.
        int count = 0;
        foreach (var b in data)
        {
            if (b is >= 0x20 and <= 0x7E)
                count++;
        }
        return count;
    }
}
