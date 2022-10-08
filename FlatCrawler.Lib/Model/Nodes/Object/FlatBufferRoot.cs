using System;
using System.Text;

namespace FlatCrawler.Lib
{
    public sealed record FlatBufferRoot : FlatBufferNodeField
    {
        public const int HeaderSize = sizeof(int);
        private const int MaxMagicLength = 4;
        private const string NO_MAGIC = "NO MAGIC";

        public string Magic { get; }
        public int MagicLength { get; }
        public override string TypeName { get => "Root"; set { } }

        private FlatBufferRoot(VTable vTable, string magic, int dataTableOffset, int vTableOffset) :
            base(0, vTable, dataTableOffset, vTableOffset)
        {
            Magic = magic;
            MagicLength = dataTableOffset == HeaderSize ? 0 : magic.Length;
        }

        public static FlatBufferRoot Read(int offset, byte[] data)
        {
            int dataTableOffset = BitConverter.ToInt32(data, offset) + offset;
            var magic = dataTableOffset == HeaderSize ? NO_MAGIC : ReadMagic(offset + HeaderSize, data);

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
            var count = Array.IndexOf<byte>(data, 0, offset, MaxMagicLength) - offset;
            return count <= -1 ? MaxMagicLength : count;
        }
    }
}
