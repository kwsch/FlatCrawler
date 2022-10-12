using System;
using System.IO;
using System.Text;
using FlatCrawler.Lib;
using FlatCrawler.Tests.Properties;
using Xunit;

namespace FlatCrawler.Tests;

public static class DumpShopData
{
    [Fact]
    public static void Crawl()
    {
        var data = Resources.shop_data;

        FlatBufferRoot root = FlatBufferRoot.Read(0, data);
        var f0 = root.ReadAsTable(data, 0);
        var f1 = root.ReadAsTable(data, 1);
        CrawlSingle(f0, data, "shop0.txt");
        CrawlMulti(f1, data, "shop1");
    }

    private static void CrawlSingle(FlatBufferTableObject array, ReadOnlySpan<byte> data, string path)
    {
        var sb = new StringBuilder();
        var count = array.Length;
        for (int i = 0; i < count; i++)
        {
            var node = array.GetEntry(i);
            var f0 = node.ReadAs<ulong>(data, 0);
            var io = node.ReadAsObject(data, 1);
            var items = io.ReadArrayAs<int>(data, 0);
            int[] arr = items.ToArray();

            sb.AppendFormat("{0:X16}", f0.Value).Append(',').AppendJoin(",", arr).AppendLine();
        }
        File.WriteAllText(path, sb.ToString());
    }

    private static void CrawlMulti(FlatBufferTableObject array, ReadOnlySpan<byte> data, string path)
    {
        var count = array.Length;
        for (int i = 0; i < count; i++)
        {
            var sb = new StringBuilder();
            var node = array.GetEntry(i);
            var f0 = node.ReadAs<ulong>(data, 0);
            var tables = node.ReadAsTable(data, 1);

            for (int t = 0; t < tables.Length; t++)
            {
                var sub = tables.GetEntry(t);
                var items = sub.ReadArrayAs<int>(data, 0);
                int[] arr = items.ToArray();

                sb.AppendFormat("{0:X16}", f0.Value).Append(',').AppendJoin(",", arr).AppendLine();
            }

            File.WriteAllText($"{path}-{i}.txt", sb.ToString());
        }
    }
}
