using System.Collections.Generic;
using System.IO;
using System.Text;
using FlatCrawler.Lib;

namespace FlatCrawler.Sandbox
{
    public static class DumpShopData
    {
        public static void Crawl(string path)
        {
            var data = File.ReadAllBytes(path);

            FlatBufferRoot root = FlatBufferRoot.Read(0, data);
            var f0 = root.ReadArrayObject(0, data);
            var f1 = root.ReadArrayObject(1, data);
            Crawl(f0, data, "shop0.txt");
            Crawl(f1, data, "shop1.txt");
        }

        private static void Crawl(FlatBufferTableObject array, byte[] data, string path)
        {
            var sb = new StringBuilder();
            var count = array.Length;
            for (int i = 0; i < count; i++)
            {
                var node = array.GetEntry(i);
                var f0 = node.ReadUInt64(0, data);
                var io = node.ReadObject(1, data);
                var items = io.ReadArrayInt32(0, data);
                int[] arr = GetArray(items);

                sb.AppendFormat("{0:X16}", f0.Value).Append(',').AppendJoin(",", arr).AppendLine();
            }
            File.WriteAllText(path, sb.ToString());
        }

        private static int[] GetArray(FlatBufferTableStruct<int> items)
        {
            var list = new List<int>();
            var count = items.Length;
            for (int i = 0; i < count; i++)
                list.Add(items.Entries[i].Value);
            return list.ToArray();
        }
    }
}
