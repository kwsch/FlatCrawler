using System;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

public sealed record FlatBufferRoot : FlatBufferNodeField
{
    public const int HeaderSize = sizeof(int);
    private const int MaxMagicLength = 4;
    private const string NO_MAGIC = "NO MAGIC";

    public string Magic { get; }
    public int MagicLength { get; }
    public int Size => HeaderSize + MagicLength;
    public override string TypeName { get => "Root"; set { } }

    private FlatBufferRoot(VTable vTable, string magic, int dataTableOffset) :
        base(0, vTable, dataTableOffset)
    {
        Magic = magic;
        MagicLength = dataTableOffset == HeaderSize ? 0 : magic.Length;
    }

    public static FlatBufferRoot Read(int offset, ReadOnlySpan<byte> data)
    {
        int dataTableOffset = ReadInt32LittleEndian(data[offset..]) + offset;
        var magic = dataTableOffset == HeaderSize ? NO_MAGIC : ReadMagic(offset + HeaderSize, data);

        // Read VTable
        var vTableOffset = GetVtableOffset(dataTableOffset, data, true);
        var vTable = ReadVTable(vTableOffset, data);
        return new FlatBufferRoot(vTable, magic, dataTableOffset);
    }

    private static string ReadMagic(int offset, ReadOnlySpan<byte> data)
    {
        var magicRegion = data.Slice(offset, 4);
        var count = GetMagicCharCount(magicRegion);
        return count == 0 ? NO_MAGIC : Encoding.ASCII.GetString(magicRegion[..count]);
    }

    private static int GetMagicCharCount(ReadOnlySpan<byte> data)
    {
        var count = data.IndexOf((byte)0);
        return count == -1 ? MaxMagicLength : count;
    }
}
