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
    private const string NO_MAGIC = "NO MAGIC";

    public string Magic { get; }
    public int MagicLength { get; }
    public override string TypeName { get => "Root"; set { } }
    private DataRange NodeMemory => new(0..Size, TypeName);

    private FlatBufferRoot(VTable vTable, string magic, int dataTableOffset) :
        base(0, vTable, dataTableOffset, null!)
    {
        Magic = magic;
        MagicLength = ((magic == NO_MAGIC) ? 0 : magic.Length);
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
        int dataTableOffset = ReadInt32LittleEndian(file.Data[offset..]) + offset;
        var magic = ((dataTableOffset == HeaderSize) ? NO_MAGIC : ReadMagic(offset + HeaderSize, file.Data));

        // Read VTable
        var vTableOffset = GetVtableOffset(dataTableOffset, file.Data, true);
        var vTable = file.PeekVTable(vTableOffset);
        file.RegisterVTable(vTable);
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
