using System;
using System.IO;
using System.Text;
using FlatCrawler.Lib;
using FlatCrawler.Tests.Properties;
using Xunit;

namespace FlatCrawler.Tests;

public static class DumpShopData
{
    /* Schema:
     * root
     * 0: shop0[]
     * 1: shop1[]
     *
     * shop0
     * 0: u64 hash
     * 1: int[] items
     *
     * shop1
     * 0: u64 hash
     * 1: inventory[]
     *
     * inventory
     * 0: int[] items
     */

    [Fact]
    public static void Crawl()
    {
        var data = Resources.shop_data;
        var file = new FlatBufferFile(data);
        var root = FlatBufferRoot.Read(file, 0);
        var f0 = root.ReadAsTable(data, 0);
        var f1 = root.ReadAsTable(data, 1);
        CrawlSingle(f0, data, "shop0.txt");
        CrawlMulti(f1, data, "shop1");
    }

    private static void CrawlSingle(FlatBufferTableObject array, ReadOnlySpan<byte> data, string path)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        var count = array.Length;
        for (int i = 0; i < count; i++)
        {
            var node = array.GetEntry(i);
            var f0 = node.ReadAs<ulong>(data, 0);
            var io = node.ReadAsObject(data, 1);
            var items = io.ReadArrayAs<int>(data, 0);
            int[] arr = items.ToArray();

            writer.WriteLine($"{f0.Value:X16},{string.Join(',', arr)}");
        }
    }

    private static void CrawlMulti(FlatBufferTableObject array, ReadOnlySpan<byte> data, string path)
    {
        var count = array.Length;
        for (int i = 0; i < count; i++)
        {
            using var writer = new StreamWriter($"{path}-{i}.txt", false, Encoding.UTF8);
            var node = array.GetEntry(i);
            var f0 = node.ReadAs<ulong>(data, 0);
            var tables = node.ReadAsTable(data, 1);

            for (int t = 0; t < tables.Length; t++)
            {
                var sub = tables.GetEntry(t);
                var items = sub.ReadArrayAs<int>(data, 0);
                int[] arr = items.ToArray();

                writer.WriteLine($"{f0.Value:X16},{string.Join(',', arr)}");
            }
        }
    }
}
