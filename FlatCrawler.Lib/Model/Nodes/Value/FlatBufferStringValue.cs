using System;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FlatCrawler.Lib;

/// <summary>
/// Node that contains a <see cref="Encoding.UTF8"/> String.
/// </summary>
public sealed record FlatBufferStringValue : FlatBufferNode
{
    public const int HeaderSize = sizeof(int);
    public const int NullTerminatorSize = sizeof(byte);
    private DataRange NodePtrMemory => new(Offset..(Offset + Size), DataCategory.Pointer, $"String Ptr ({Name} @ 0x{StringOffset:X})", true);
    private DataRange StringMemory => new(StringOffset..(StringOffset + StringLength), DataCategory.Value, $"{Name} String Value");
    private DataRange StringLengthMemory => new(StringLengthOffset..(StringLengthOffset + HeaderSize), DataCategory.Misc, $"{Name} String Length ({StringLength})");
    private int AlignedPadding => (int)MemoryUtil.AlignToBytes((uint)StringMemory.End + NullTerminatorSize, 2);

    // > Strings end within the buffer and has a zero byte after the end which is also within the buffer.
    // See (https://github.com/dvidelabs/flatcc/blob/master/doc/binary-format.md#verification)
    private DataRange NullTerminatorMemory => new(StringMemory.End..AlignedPadding, DataCategory.Padding, $"{Name} Null Terminator + Pad");


    public override string TypeName { get => $"string ({Value})"; set { } }

    /// <summary>
    /// Absolute offset that has the raw string bytes.
    /// </summary>
    public int StringOffset { get; }

    /// <summary> Absolute offset that has the count of bytes of the string. </summary>
    public int StringLengthOffset { get; }

    /// <summary> The length of the string in bytes. </summary>
    public int StringLength { get; }

    /// <summary> The value of the string. </summary>
    public string Value { get; }

    public bool IsReadable
    {
        get
        {
            foreach (var c in Value)
            {
                if (char.IsControl(c))
                    return false;
            }
            return true;
        }
    }

    private FlatBufferStringValue(int definedOffset, int stringLengthOffset, int stringOffset, int stringLength, string str, FlatBufferNode parent) :
        base(definedOffset, parent)
    {
        StringOffset = stringOffset;
        StringLengthOffset = stringLengthOffset;
        StringLength = stringLength;
        Value = str;
    }

    public override void RegisterMemory()
    {
        FbFile.SetProtectedMemory(NodePtrMemory);
        FbFile.SetProtectedMemory(StringMemory);
        FbFile.SetProtectedMemory(StringLengthMemory);
        FbFile.SetProtectedMemory(NullTerminatorMemory);
    }

    public override void UnRegisterMemory()
    {
        FbFile.RemoveProtectedMemory(NodePtrMemory);
        FbFile.RemoveProtectedMemory(StringMemory);
        FbFile.RemoveProtectedMemory(StringLengthMemory);
        FbFile.RemoveProtectedMemory(NullTerminatorMemory);
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
