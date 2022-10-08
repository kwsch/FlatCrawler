using System;
using System.Collections.Generic;
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
        var f0 = root.ReadArrayObject(0, data);
        var f1 = root.ReadArrayObject(1, data); // union table, yuck
        var f2 = root.ReadArrayObject(2, data);

        DumpFirstTable(f0, data);
        DumpTypes(f1, data);
        DumpMemoryTable(f1, data);
        DumpThirdTable(f2, data);
    }

    private static void DumpFirstTable(FlatBufferTableObject node, byte[] data)
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
                var value = entry.ReadInt32(f, data);
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

    private static void DumpTypes(FlatBufferTableObject node, byte[] data)
    {
        var count = node.Length;
        List<byte> result = new();
        for (int i = 0; i < count; i++)
        {
            var entry = node.GetEntry(i);
            var type0 = entry.ReadUInt8(0, data);
            // Console.WriteLine($"{i:0000} - {type0.Value}");
            result.Add(type0.Value);
        }
        File.WriteAllBytes("types.bin", result.ToArray());
    }

    private static void DumpMemoryTable(FlatBufferTableObject node, byte[] data)
    {
        var count = node.Length;
        var sb = new StringBuilder(1 << 17);
        for (int i = 0; i < count; i++)
        {
            var entry = node.GetEntry(i);
            var type0 = entry.ReadUInt8(0, data).Value;
            var node1 = entry.ReadObject(1, data);
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

    private static object GetValue(byte type, FlatBufferNodeField obj, byte[] data) => type switch
    {
        1 => obj.ReadUInt8(0, data).Value,
        3 => obj.ReadString(0, data).Value,
        4 => obj.ReadUInt64(0, data).Value.ToString("X16"),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private static void DumpThirdTable(FlatBufferTableObject node, byte[] data)
    {
        var count = node.Length;
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            var entry = node.GetEntry(i);
            var hash = entry.ReadUInt64(0, data).Value;
            var node1 = entry.ReadArrayObject(1, data);
            for (int j = 0; j < node1.Length; j++)
            {
                var obj = node1.GetEntry(j);
                var hash0 = obj.ReadUInt64(0, data).Value;
                var hash1 = obj.ReadUInt64(1, data).Value;
                sb.AppendFormat("{0:X16}", hash).Append(", ").AppendFormat("{0:X16}", hash0).Append(", ").AppendFormat("{0:X16}", hash1).AppendLine();
            }
        }
        File.WriteAllText("table2.txt", sb.ToString());
    }
}
