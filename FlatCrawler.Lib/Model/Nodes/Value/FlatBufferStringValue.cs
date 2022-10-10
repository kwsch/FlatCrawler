using System;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

public sealed record FlatBufferStringValue : FlatBufferNode
{
    public override string TypeName { get => $"string ({Value})"; set { } }
    public int StringOffset { get; }
    public int StringLengthOffset { get; }
    public int StringLength { get; }
    public string Value { get; }

    public const int HeaderSize = sizeof(int);
    public const int NullTerminatorSize = sizeof(char);

    private FlatBufferStringValue(int definedOffset, int stringLengthOffset, int stringOffset, int stringLength, string str, FlatBufferNode parent) :
        base(definedOffset, parent)
    {
        StringOffset = stringOffset;
        StringLengthOffset = stringLengthOffset;
        StringLength = stringLength;
        Value = str;
    }

    public static FlatBufferStringValue Read(int offset, FlatBufferNode parent, ReadOnlySpan<byte> data)
    {
        var encodedOffset = ReadInt32LittleEndian(data[offset..]) + offset;
        var len = ReadInt32LittleEndian(data[encodedOffset..]);
        int charOffset = encodedOffset + HeaderSize;
        var str = Encoding.UTF8.GetString(data.Slice(charOffset, len));
        return new FlatBufferStringValue(offset, encodedOffset, charOffset, len, str, parent);
    }

    public static FlatBufferStringValue Read(FlatBufferNodeField parent, int fieldIndex, ReadOnlySpan<byte> data) => Read(parent.GetFieldOffset(fieldIndex), parent, data);
}
