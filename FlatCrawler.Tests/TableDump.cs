using System;
using System.IO;
using System.Text;
using FlatCrawler.Lib;
using FlatCrawler.Tests.Properties;
using Xunit;

namespace FlatCrawler.Sandbox;

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
        FlatBufferRoot root = FlatBufferRoot.Read(0, data);
        var f1 = root.ReadArrayObject(1, data); // union table, yuck

        DumpFoodTable(f1, data, cellCount, name);
    }

    private static void DumpFoodTable(FlatBufferTableObject node, ReadOnlySpan<byte> data, int cellsPerRow, string name)
    {
        var count = node.Length;
        var sb = new StringBuilder(1 << 17);
        for (int i = 0; i < count; i++)
        {
            var entry = node.GetEntry(i);
            var type0 = entry.ReadUInt8(0, data).Value;
            var node1 = entry.ReadObject(1, data);
            var value = node1 is IFieldNode { AllFields.Count: 0 } ? "0" : GetValue(type0, node1, data);

            bool start = i % cellsPerRow == 0;
            if (!start)
                sb.Append('\t');
            sb.Append(value);

            if (i % cellsPerRow == cellsPerRow-1)
                sb.AppendLine();
        }

        File.WriteAllText($"{name}.txt", sb.ToString());
    }

    private static object GetValue(byte type, FlatBufferNodeField obj, ReadOnlySpan<byte> data) => type switch
    {
        1 => obj.ReadUInt8(0, data).Value,
        3 => obj.ReadString(0, data).Value,
        4 => obj.ReadUInt64(0, data).Value.ToString("X16"),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
