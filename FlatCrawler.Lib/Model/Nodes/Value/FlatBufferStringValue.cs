using System;
using System.Text;

namespace FlatCrawler.Lib
{
    public sealed record FlatBufferStringValue : FlatBufferNode
    {
        public override string TypeName { get => $"string ({Value})"; set { } }
        public int StringOffset { get; }
        public int StringLengthOffset { get; }
        public int StringLength { get; }
        public string Value { get; }

        private FlatBufferStringValue(int definedOffset, int stringOffset, int stringLengthOffset, int stringLength, string str, FlatBufferNode parent) : base(definedOffset, parent)
        {
            StringOffset = stringOffset;
            StringLengthOffset = stringLengthOffset;
            StringLength = stringLength;
            Value = str;
        }

        public static FlatBufferStringValue Read(int offset, FlatBufferNode parent, byte[] data)
        {
            var encodedOffset = BitConverter.ToInt32(data, offset) + offset;
            var len = BitConverter.ToInt32(data, encodedOffset);
            int charOffset = encodedOffset + 4;
            var str = Encoding.UTF8.GetString(data, charOffset, len);
            return new FlatBufferStringValue(offset, encodedOffset, charOffset, len, str, parent);
        }

        public static FlatBufferStringValue Read(FlatBufferNodeField parent, int fieldIndex, byte[] data) => Read(parent.GetFieldOffset(fieldIndex), parent, data);
    }
}
