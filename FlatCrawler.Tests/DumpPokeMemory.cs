using System;
using System.IO;
using System.Text;
using FlatCrawler.Lib;
using FlatCrawler.Tests.Properties;
using Xunit;

namespace FlatCrawler.Sandbox;

public static class DumpPokeMemory
{
    [Fact]
    public static void Crawl()
    {
        var data = Resources.poke_memory;

        FlatBufferRoot root = FlatBufferRoot.Read(0, data);
        var f0 = root.ReadAsTable(data, 0);
        var f1 = root.ReadAsTable(data, 1); // union table, yuck
        var f2 = root.ReadAsTable(data, 2);

        DumpFirstTable(f0, data);
        DumpTypes(f1, data);
        DumpMemoryTable(f1, data);
        DumpThirdTable(f2, data);
    }

    private static void DumpFirstTable(FlatBufferTableObject node, ReadOnlySpan<byte> data)
    {
        var count = node.Length;
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            var entry = node.GetEntry(i);
            var fc = entry.AllFields.Count;
            for (int f = 0; f < fc; f++)
            {
                if (!entry.HasField(f))
                {
                    sb.Append("0,");
                    continue;
                }
                var value = entry.ReadAs<int>(data, f);
                sb.Append(value).Append(',');
            }

            for (int f = fc; f < 4; f++)
            {
                sb.Append("0,");
            }
            sb.AppendLine();
        }
        File.WriteAllText("table0.txt", sb.ToString());
    }

    private static void DumpTypes(FlatBufferTableObject node, ReadOnlySpan<byte> data)
    {
        var result = node.GetUnionTypes(data);
        File.WriteAllBytes("types.bin", result);
    }

    private static void DumpMemoryTable(FlatBufferTableObject node, ReadOnlySpan<byte> data)
    {
        var count = node.Length;
        var sb = new StringBuilder(1 << 17);
        for (int i = 0; i < count; i++)
        {
            var entry = node.GetEntry(i);
            var type0 = entry.ReadAs<byte>(data, 0).Value;
            var node1 = entry.ReadAsObject(data, 1);
            var value = node1 is IFieldNode { AllFields.Count: 0 } ? "0" : GetValue(type0, node1, data);

            bool start = i % 0x1F == 0;
            if (!start)
                sb.Append('\t');
            sb.Append(value);

            if (i % 0x1F == 0x1E)
                sb.AppendLine();
        }

        File.WriteAllText("table1.txt", sb.ToString());
    }

    private static object GetValue(byte type, FlatBufferNodeField obj, ReadOnlySpan<byte> data) => type switch
    {
        1 => obj.ReadAs<byte>(data, 0).Value,
        3 => obj.ReadAsString(data, 0).Value,
        4 => obj.ReadAs<ulong>(data, 0).Value.ToString("X16"),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private static void DumpThirdTable(FlatBufferTableObject node, ReadOnlySpan<byte> data)
    {
        var count = node.Length;
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            var entry = node.GetEntry(i);
            var hash = entry.ReadAs<ulong>(data, 0).Value;
            var node1 = entry.ReadAsTable(data, 1);
            for (int j = 0; j < node1.Length; j++)
            {
                var obj = node1.GetEntry(j);
                var hash0 = obj.ReadAs<ulong>(data, 0).Value;
                var hash1 = obj.ReadAs<ulong>(data, 1).Value;
                sb.AppendFormat("{0:X16}", hash).Append(", ").AppendFormat("{0:X16}", hash0).Append(", ").AppendFormat("{0:X16}", hash1).AppendLine();
            }
        }
        File.WriteAllText("table2.txt", sb.ToString());
    }
}
