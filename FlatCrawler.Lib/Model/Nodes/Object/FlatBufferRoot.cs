using System;
using System.Text;

namespace FlatCrawler.Lib
{
    public sealed record FlatBufferRoot : FlatBufferNodeField
    {
        public string Magic { get; }
        public override string TypeName { get => "Root"; set { } }

        private const string NO_MAGIC = "NO MAGIC";

        private FlatBufferRoot(VTable vTable, string magic, int dataTableOffset, int vTableOffset) : base(0, vTable, dataTableOffset, vTableOffset)
        {
            Magic = magic;
        }

        public static FlatBufferRoot Read(int offset, byte[] data)
        {
            int dataTableOffset = BitConverter.ToInt32(data, offset) + offset;
            var magic = dataTableOffset == 4 ? NO_MAGIC : ReadMagic(offset + 4, data);

            // Read VTable
            var vTableOffset = GetVtableOffset(dataTableOffset, data, true);
            var vTable = ReadVTable(vTableOffset, data);
            return new FlatBufferRoot(vTable, magic, dataTableOffset, vTableOffset);
        }

        private static string ReadMagic(int offset, byte[] data)
        {
            var count = GetMagicCharCount(offset, data);
            return count == 0 ? NO_MAGIC : Encoding.ASCII.GetString(data, offset, count);
        }

        private static int GetMagicCharCount(int offset, byte[] data)
        {
            var count = Array.IndexOf<byte>(data, 0, offset, 4) - offset;
            return count <= -1 ? 4 : count;
        }
    }
}
