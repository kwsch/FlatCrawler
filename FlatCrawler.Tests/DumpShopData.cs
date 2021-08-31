using System.Collections.Generic;
using System.IO;
using System.Text;
using FlatCrawler.Lib;
using FlatCrawler.Tests.Properties;
using Xunit;

namespace FlatCrawler.Sandbox
{
    public static class DumpShopData
    {
        [Fact]
        public static void Crawl()
        {
            var data = Resources.shop_data;

            FlatBufferRoot root = FlatBufferRoot.Read(0, data);
            var f0 = root.ReadArrayObject(0, data);
            var f1 = root.ReadArrayObject(1, data);
            CrawlSingle(f0, data, "shop0.txt");
            CrawlMulti(f1, data, "shop1");
        }

        private static void CrawlSingle(FlatBufferTableObject array, byte[] data, string path)
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

        private static void CrawlMulti(FlatBufferTableObject array, byte[] data, string path)
        {
            var count = array.Length;
            for (int i = 0; i < count; i++)
            {
                var sb = new StringBuilder();
                var node = array.GetEntry(i);
                var f0 = node.ReadUInt64(0, data);
                var tables = node.ReadArrayObject(1, data);

                for (int t = 0; t < tables.Length; t++)
                {
                    var sub = tables.GetEntry(t);
                    var items = sub.ReadArrayInt32(0, data);
                    int[] arr = GetArray(items);

                    sb.AppendFormat("{0:X16}", f0.Value).Append(',').AppendJoin(",", arr).AppendLine();
                }

                File.WriteAllText($"{path}-{i}.txt", sb.ToString());
            }
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
