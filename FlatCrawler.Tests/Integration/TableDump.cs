using System;
using System.IO;
using System.Text;
using FlatCrawler.Lib;
using FlatCrawler.Tests.Properties;
using Xunit;

namespace FlatCrawler.Tests;

public static class TableDump
{
    [Fact]
    public static void Crawl()
    {
        DumpFoodTable(Resources.pokecamp_foodstuff_table, 2, "foodstuff");
        DumpFoodTable(Resources.pokecamp_kinomi_table, 7, "kinomi");
    }

    private static void DumpFoodTable(ReadOnlySpan<byte> data, int cellCount, string name)
    {
        var file = new FlatBufferFile(data);
        var root = FlatBufferRoot.Read(file, 0);
        var f1 = root.ReadAsTable(data, 1); // union table, yuck

        DumpFoodTable(f1, data, cellCount, name);
    }

    private static void DumpFoodTable(FlatBufferTableObject node, ReadOnlySpan<byte> data, int cellsPerRow, string name)
    {
        var count = node.Length;
        var sb = new StringBuilder(1 << 17);
        for (int i = 0; i < count; i++)
        {
            var entry = node.GetEntry(i);
            var type0 = entry.ReadAs<byte>(data, 0).Value;
            var node1 = entry.ReadAsObject(data, 1);
            var value = node1 is IFieldNode { AllFields.Count: 0 } ? "0" : GetValue(type0, node1, data);

            bool start = i % cellsPerRow == 0;
            if (!start)
                sb.Append('\t');
            sb.Append(value);

            if (i % cellsPerRow == cellsPerRow - 1)
                sb.AppendLine();
        }

        File.WriteAllText($"{name}.txt", sb.ToString());
    }

    private static object GetValue(byte type, FlatBufferNodeField obj, ReadOnlySpan<byte> data) => type switch
    {
        1 => obj.ReadAs<byte>(data, 0).Value,
        3 => obj.ReadAsString(data, 0).Value,
        4 => obj.ReadAs<ulong>(data, 0).Value.ToString("X16"),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
