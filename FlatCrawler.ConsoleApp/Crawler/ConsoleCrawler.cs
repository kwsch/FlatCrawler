using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FlatCrawler.Lib;

namespace FlatCrawler.ConsoleApp
{
    public class ConsoleCrawler
    {
        private readonly List<string> ProcessedCommands = new();
        private const string SaveStatePath = "lines.txt";

        private readonly byte[] Data;
        private readonly string FilePath;

        public ConsoleCrawler(string path, byte[] data)
        {
            FilePath = path;
            Data = data;
        }

        public void CrawlLoop()
        {
            Console.WriteLine($"Crawling {Path.GetFileName(FilePath)}...");
            Console.WriteLine();

            FlatBufferNode node = FlatBufferRoot.Read(0, Data);
            node.PrintTree();

            while (true)
            {
                Console.Write(">>> ");
                var cmd = Console.ReadLine();
                if (cmd is null)
                    break;
                var result = ProcessCommand(cmd, ref node, Data);
                if (result == CrawlResult.Quit)
                    break;

                Console.WriteLine();
                if (result == CrawlResult.Unrecognized)
                    Console.WriteLine($"Try again... unable to recognize command: {cmd}");
                else if (result == CrawlResult.Error)
                    Console.WriteLine($"Try again... parsing/executing that command didn't work: {cmd}");
                else if (result != CrawlResult.Silent)
                    ProcessedCommands.Add(cmd);

                if (result.IsSavedNavigation())
                    node.PrintTree();
            }
        }

        private CrawlResult ProcessCommand(string cmd, ref FlatBufferNode node, byte[] data)
        {
            var sp = cmd.IndexOf(' ');
            if (sp == -1)
                return ProcessCommandSingle(cmd.ToLowerInvariant(), ref node, data);
            var c = cmd[..sp].ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(c))
                return CrawlResult.Unrecognized;

            var args = cmd[(sp + 1)..];
            try
            {
                switch (c)
                {
                    case "ro" when node is IFieldNode p:
                    {
                        var fieldIndex = int.Parse(args.Replace("0x", ""), NumberStyles.HexNumber);
                        var ofs = p.GetReferenceOffset(fieldIndex, data);
                        Console.WriteLine($"Offset: 0x{ofs:X}");
                        return CrawlResult.Silent;
                    }
                    case "fo" when node is IFieldNode p:
                    {
                        var fieldIndex = int.Parse(args.Replace("0x", ""), NumberStyles.HexNumber);
                        var ofs = p.GetFieldOffset(fieldIndex);
                        Console.WriteLine($"Offset: 0x{ofs:X}");
                        return CrawlResult.Silent;
                    }
                    case "eo" when node is IArrayNode p:
                    {
                        var fieldIndex = int.Parse(args.Replace("0x", ""), NumberStyles.HexNumber);
                        var ofs = p.GetEntry(fieldIndex).Offset;
                        Console.WriteLine($"Offset: 0x{ofs:X}");
                        return CrawlResult.Silent;
                    }
                    case "rf" when node is IFieldNode p:
                    {
                        if (!args.Contains(' '))
                        {
                            node = p.GetField(int.Parse(args)) ?? throw new ArgumentNullException(nameof(FlatBufferNode), "node not explored yet.");
                            return CrawlResult.Silent;
                        }

                        var (fieldIndex, fieldType) = CommandUtil.GetDualArgs(args);
                        node = ReadNode(node, fieldIndex, fieldType.ToLowerInvariant(), data);
                        return CrawlResult.Navigate;
                    }
                    case "rf" when node is IArrayNode p:
                    {
                        if (!args.Contains(' '))
                        {
                            node = p.GetEntry(int.Parse(args));
                            return CrawlResult.Navigate;
                        }

                        var (fieldIndex, fieldType) = CommandUtil.GetDualArgs(args);
                        node = ReadNode(node, fieldIndex, fieldType.ToLowerInvariant(), data);
                        return CrawlResult.Navigate;
                    }
                    case "rf":
                    {
                        Console.WriteLine("Node has no fields. Unable to read the requested field node.");
                        return CrawlResult.Silent;
                    }
                    case "fewf" when node is IArrayNode p:
                    {
                        var fIndex = int.Parse(args);
                        for (int i = 0; i < p.Entries.Count; i++)
                        {
                            if (p.Entries[i] is not FlatBufferNodeField f || !f.HasField(fIndex))
                                continue;
                            Console.WriteLine($"Entry {i} has a value for Field {fIndex}");
                            return CrawlResult.Silent;
                        }
                        Console.WriteLine("No entry has a value for that field.");
                        return CrawlResult.Silent;
                    }
                    case "fewfs" when node is IArrayNode p:
                    {
                        var fIndex = int.Parse(args);
                        var result = new List<int>();
                        for (int i = 0; i < p.Entries.Count; i++)
                        {
                            if (p.Entries[i] is not FlatBufferNodeField f || !f.HasField(fIndex))
                                continue;
                            result.Add(i);
                        }
                        Console.WriteLine(result.Count != 0
                            ? $"Entries having a value for field {fIndex}: {string.Join(" ", result)}"
                            : "No entry has a value for that field.");
                        return CrawlResult.Silent;
                    }

                    case "hex" or "h":
                    {
                        if (!int.TryParse(args.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexOffset))
                        {
                            Console.WriteLine("Unable to parse hex offset.");
                            return CrawlResult.Silent;
                        }

                        DumpHex(data, hexOffset);
                        return CrawlResult.Silent;
                    }
                    default:
                        return CrawlResult.Unrecognized;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return CrawlResult.Error;
            }
        }

        private static void DumpHex(byte[] data, int offset)
        {
            Console.WriteLine($"Requested offset: 0x{offset:X8}");
            var dump = HexDumper.Dump(data, offset);
            Console.WriteLine(dump);
        }

        private CrawlResult ProcessCommandSingle(string cmd, ref FlatBufferNode node, byte[] data)
        {
            try
            {
                switch (cmd)
                {
                    case "tree":
                        node.PrintTree();
                        return CrawlResult.Silent;
                    case "load":
                        foreach (var line in File.ReadLines(SaveStatePath))
                            ProcessCommand(line, ref node, data);
                        Console.WriteLine("Reloaded state.");
                        return CrawlResult.Silent;
                    case "dump":
                        File.WriteAllLines(SaveStatePath, ProcessedCommands);
                        return CrawlResult.Silent;
                    case "clear":
                        Console.Clear();
                        return CrawlResult.Silent;
                    case "quit":
                        return CrawlResult.Quit;
                    case "p" or "info" or "print":
                        node.Print();
                        return CrawlResult.Silent;
                    case "hex" or "h":
                        DumpHex(data, node.Offset);
                        return CrawlResult.Silent;
                    case "au" or "analyze" or "union" when node is IArrayNode a:
                        AnalyzeUnion(data, a);
                        return CrawlResult.Navigate;

                    case "mfc" when node is IArrayNode an:
                    {
                        int index = 0;
                        int max = 0;
                        for (int i = 0; i < an.Entries.Count; i++)
                        {
                            if (an.Entries[i] is not FlatBufferNodeField f || f.FieldCount <= max)
                                continue;
                            index = i;
                            max = f.FieldCount;
                        }
                        Console.WriteLine(max != 0
                            ? $"Max field count is {max} @ index {index}"
                            : "No nodes have a detectable field count.");
                        return CrawlResult.Silent;
                    }
                    case "up":
                        if (node.Parent is not { } up)
                        {
                            Console.WriteLine("Node has no parent. Unable to go up.");
                            return CrawlResult.Silent;
                        }
                        node = up;
                        return CrawlResult.Navigate;
                    case "root":
                        if (node.Parent is null)
                        {
                            Console.WriteLine("Node is already root. Unable to go up.");
                            return CrawlResult.Silent;
                        }
                        do { node = node.Parent; } while (node.Parent is { });
                        Console.WriteLine($"Success! Reset to root @ offset 0x{node.Offset}");
                        return CrawlResult.Navigate;
                    default:
                        return CrawlResult.Unrecognized;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return CrawlResult.Error;
            }
        }

        private static void AnalyzeUnion(byte[] data, IArrayNode array)
        {
            var result = new Dictionary<byte, Union>();
            var entries = array.Entries;
            for (var index = 0; index < entries.Count; index++)
            {
                var flatBufferNode = entries[index];
                var node = (FlatBufferObject)flatBufferNode;
                var type = node.GetFieldValue(0, data, TypeCode.Byte);
                var obj = node.ReadObject(1, data);
                var bval = ((FlatBufferFieldValue<byte>)type).Value;
                var chk = new Union(bval, index, obj.FieldCount);
                node.ForceNameHint(chk.ToString());

                // add or update key if our FieldCount is new or bigger than previously noted for this union type
                if (!result.TryGetValue(bval, out var c) || c.FieldCount < chk.FieldCount)
                    result[bval] = chk;
            }

            var unique = result.OrderBy(z => z.Key).ToArray();
            Console.WriteLine($"Unique Types: {string.Join(" ", unique.Select(z => $"{z.Key}"))}");
            Console.WriteLine($"Example Type Indexes:{Environment.NewLine}{string.Join(Environment.NewLine, unique.Select(z => $"{z.Value}"))}");
        }

        private sealed record Union(byte Type, int Index, int FieldCount);

        private static FlatBufferNode ReadNode(FlatBufferNode node, int fieldIndex, string type, byte[] data) => node switch
        {
            IArrayNode a => a.GetEntry(fieldIndex),
            FlatBufferNodeField r => ReadNode(r, fieldIndex, data, type),
            _ => throw new ArgumentException("Field not present in VTable"),
        };

        private static FlatBufferNode ReadNode(FlatBufferNodeField node, int fieldIndex, byte[] data, string type)
        {
            FlatBufferNode result = type switch
            {
                "string" or "str"     => node.ReadString(fieldIndex, data),
                "object"              => node.ReadObject(fieldIndex, data),

                "table" or "object[]" => node.ReadArrayObject(fieldIndex, data),
                "string[]"            => node.ReadArrayString(fieldIndex, data),

                _ => GetStructureNode(node, fieldIndex, data, type),
            };
            node.SetFieldHint(fieldIndex, type);
            node.TrackChildFieldNode(fieldIndex, result);
            return result;
        }

        private static FlatBufferNode GetStructureNode(FlatBufferNodeField node, int fieldIndex, byte[] data, string type)
        {
            var typecode = CommandUtil.GetTypeCode(type);
            if (type.Contains("[]")) // table-array
                return node.GetTableStruct(fieldIndex, data, typecode);
            return node.GetFieldValue(fieldIndex, data, typecode);
        }
    }
}
