using System;
using System.IO;
using System.Text;
using FlatCrawler.Lib;

namespace FlatCrawler.Tests;

public static class SwordShieldPRMB
{
    public static void Export(byte[] data, string fileNamePrefix, int tableWidth)
    {
        var file = new FlatBufferFile(data);
        Export(file, fileNamePrefix, tableWidth);
    }

    public static void Export(FlatBufferFile file, string fileNamePrefix, int tableWidth)
    {
        var data = file.Data;
        var root = FlatBufferRoot.Read(file, 0);
        var f0 = root.ReadAsTable(data, 0);
        var f1 = root.ReadAsTable(data, 1); // union table, yuck
        var f2 = root.ReadAsTable(data, 2);

        DumpTypes(f1, data, $"types-{fileNamePrefix}.bin");

        using var prmb0 = new StreamWriter($"{fileNamePrefix}-table0.txt", false, Encoding.UTF8);
        using var prmb1 = new StreamWriter($"{fileNamePrefix}-table1.txt", false, Encoding.UTF8);
        using var prmb2 = new StreamWriter($"{fileNamePrefix}-table2.txt", false, Encoding.UTF8);
        DumpPRMB_0(f0, data, prmb0);
        DumpPRMB_1(f1, data, prmb1, tableWidth);
        DumpPRMB_2(f2, data, prmb2);
    }

    private static void DumpTypes(FlatBufferTableObject node, ReadOnlySpan<byte> data, string fileName)
    {
        var result = node.GetUnionTypes(data);
        File.WriteAllBytes(fileName, result);
    }

    private static object GetUnionTypeValue(byte type, FlatBufferNodeField obj, ReadOnlySpan<byte> data) => type switch
    {
        1 => obj.ReadAs<byte>(data, 0).Value,
        3 => obj.ReadAsString(data, 0).Value,
        4 => obj.ReadAs<ulong>(data, 0).Value.ToString("X16"),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private static void DumpPRMB_0(FlatBufferTableObject node, ReadOnlySpan<byte> data, StreamWriter writer)
    {
        var count = node.Length;
        for (int i = 0; i < count; i++)
        {
            var entry = node.GetEntry(i);
            var fc = entry.AllFields.Count;
            for (int f = 0; f < fc; f++)
            {
                if (!entry.HasField(f))
                {
                    writer.Write("0,");
                    continue;
                }

                var value = entry.ReadAs<int>(data, f);
                writer.Write(value.Value);
                writer.Write(',');
            }

            for (int f = fc; f < 4; f++)
                writer.Write("0,");
            writer.WriteLine();
        }
    }

    private static void DumpPRMB_1(FlatBufferTableObject node, ReadOnlySpan<byte> data, TextWriter writer, int tableWidth)
    {
        var count = node.Length;
        for (int i = 0; i < count; i++)
        {
            var entry = node.GetEntry(i);
            var type0 = entry.ReadAs<byte>(data, 0).Value;
            var node1 = entry.ReadAsObject(data, 1);
            var value = node1 is IFieldNode { AllFields.Count: 0 } ? "0" : GetUnionTypeValue(type0, node1, data);

            bool start = i % tableWidth == 0;
            if (!start)
                writer.Write('\t');
            writer.Write(value);

            if (i % tableWidth == tableWidth - 1)
                writer.WriteLine();
        }
    }

    private static void DumpPRMB_2(FlatBufferTableObject node, ReadOnlySpan<byte> data, TextWriter writer)
    {
        var count = node.Length;
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
                writer.WriteLine($"{hash:X16},{hash0:X16},{hash1:X16}");
            }
        }
    }
}
