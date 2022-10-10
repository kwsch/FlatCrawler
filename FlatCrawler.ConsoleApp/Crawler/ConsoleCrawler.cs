using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FlatCrawler.Lib;

namespace FlatCrawler.ConsoleApp;

public class ConsoleCrawler
{
    private readonly List<string> ProcessedCommands = new();
    private const string SaveStatePath = "lines.txt";

    private readonly string FilePath;

    public ConsoleCrawler(string path, byte[] data)
    {
        FilePath = path;
        CommandUtil.Data = data;
    }

    public void CrawlLoop()
    {
        var fn = Path.GetFileName(FilePath);
        Console.WriteLine($"Crawling {Console.Title = fn}...");
        Console.WriteLine();

        var data = CommandUtil.Data;
        FlatBufferNode node = FlatBufferRoot.Read(0, data);
        node.PrintTree();

        Console.OutputEncoding = Encoding.UTF8; // japanese strings will show up as boxes rather than ????
        while (true)
        {
            Console.Write(">>> ");
            var cmd = Console.ReadLine();
            if (cmd is null)
                break;
            var result = ProcessCommand(cmd, ref node, data);
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

    private static FlatBufferNode? GetNodeAtIndex(FlatBufferNode parent, string index)
    {
        if (parent is IFieldNode fn)
        {
            var fieldIndex = CommandUtil.GetIntPossibleHex(index);
            return fn.GetField(fieldIndex) ?? throw new ArgumentNullException(nameof(FlatBufferNode), "node not explored yet.");
        }
        if (parent is IArrayNode an)
        {
            return an.GetEntry(int.Parse(index));
        }
        return null;
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
                case "n" or "name" or "fieldname":
                {
                    if (!args.Contains(' '))
                    {
                        node.Name = args;
                        return CrawlResult.Update;
                    }

                    var argSplit = args.Split(' ');
                    var toRename = GetNodeAtIndex(node, argSplit[0]);

                    if (toRename != null)
                        toRename.Name = argSplit[1];

                    return CrawlResult.Update;
                }
                case "t" or "type" or "typename":
                {
                    if (!args.Contains(' '))
                    {
                        node.TypeName = args;
                        return CrawlResult.Update;
                    }

                    var argSplit = args.Split(' ');
                    var toRename = GetNodeAtIndex(node, argSplit[0]);

                    if (toRename != null)
                        toRename.TypeName = argSplit[1];
                    return CrawlResult.Update;
                }
                case "ro" when node is IFieldNode p:
                {
                    var fieldIndex = CommandUtil.GetIntPossibleHex(args);
                    var ofs = p.GetReferenceOffset(fieldIndex, data);
                    Console.WriteLine($"Offset: 0x{ofs:X}");
                    return CrawlResult.Silent;
                }
                case "fo" when node is IFieldNode p:
                {
                    var fieldIndex = CommandUtil.GetIntPossibleHex(args);
                    var ofs = p.GetFieldOffset(fieldIndex);
                    Console.WriteLine($"Offset: 0x{ofs:X}");
                    return CrawlResult.Silent;
                }
                case "eo" when node is IArrayNode p:
                {
                    var fieldIndex = CommandUtil.GetIntPossibleHex(args);
                    var ofs = p.GetEntry(fieldIndex).Offset;
                    Console.WriteLine($"Offset: 0x{ofs:X}");
                    return CrawlResult.Silent;
                }
                case "rf" when node is IFieldNode p:
                {
                    if (!args.Contains(' '))
                    {
                        var index = CommandUtil.GetIntPossibleHex(args);
                        node = p.GetField(index) ?? throw new ArgumentNullException(nameof(FlatBufferNode), "node not explored yet.");
                        return CrawlResult.Navigate;
                    }

                    var (fieldIndex, fieldType) = CommandUtil.GetDualArgs(args);
                    var result = node.ReadNode(fieldIndex, fieldType.ToLowerInvariant(), data);
                    if (result is not (IStructNode or FlatBufferStringValue))
                        node = result;
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
                    var result = node.ReadNode(fieldIndex, fieldType.ToLowerInvariant(), data);
                    if (result is not (IStructNode or FlatBufferStringValue))
                        node = result;
                    return CrawlResult.Navigate;
                }
                case "rf":
                {
                    Console.WriteLine("Node has no fields. Unable to read the requested field node.");
                    return CrawlResult.Silent;
                }
                case "fowf" when node is IArrayNode p:
                {
                    var (objectIndex, other) = CommandUtil.GetDualArgs(args);
                    var fieldIndex = int.Parse(other);
                    for (int i = 0; i < p.Entries.Count; i++)
                    {
                        var x = p.GetEntry(i);
                        var y = x.ReadNode(objectIndex, "object", data);
                        var fc = ((FlatBufferNodeField)y).HasField(fieldIndex);
                        if (!fc)
                            continue;
                        Console.WriteLine($"Entry {i} has an object at field {objectIndex} with a value for its Field {fieldIndex}");
                        return CrawlResult.Silent;
                    }
                    Console.WriteLine("Node has no fields. Unable to read the requested field node.");
                    return CrawlResult.Silent;
                }
                case "fewf" when node is IArrayNode p:
                {
                    var fIndex = int.Parse(args);
                    var result = p.GetEntryIndexWithField(fIndex);
                    Console.WriteLine(result != -1
                        ? $"Entry {result} has a value for Field {fIndex}"
                        : "No entry has a value for that field.");
                    return CrawlResult.Silent;
                }
                case "fewfs" when node is IArrayNode p:
                {
                    var fIndex = int.Parse(args);
                    var result = p.GetEntryIndexesWithField(fIndex);
                    Console.WriteLine(result.Count != 0
                        ? $"Entries having a value for field {fIndex}: {string.Join(" ", result)}"
                        : "No entry has a value for that field.");
                    return CrawlResult.Silent;
                }
                case "of" when node is FlatBufferNodeField f:
                {
                    var offset = int.Parse(args.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    var index = f.VTable.GetFieldIndex(offset - f.DataTableOffset);
                    Console.WriteLine($"Offset {offset:X} is Field {index}");
                    return CrawlResult.Silent;
                }

                case "hex" or "h":
                {
                    var offset = int.Parse(args.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    DumpHex(data, offset);
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
                case "clear":
                    Console.Clear();
                    return CrawlResult.Silent;
                case "path":
                    Console.WriteLine(FilePath);
                    return CrawlResult.Silent;
                case "quit":
                    return CrawlResult.Quit;
                case "p" or "info" or "print":
                    node.Print();
                    return CrawlResult.Silent;
                case "hex" or "h":
                    DumpHex(data, node.Offset);
                    return CrawlResult.Silent;

                // reloading state from previous session
                case "dump":
                    File.WriteAllLines(SaveStatePath, ProcessedCommands);
                    return CrawlResult.Silent;
                case "load":
                    foreach (var line in File.ReadLines(SaveStatePath))
                        ProcessCommand(line, ref node, data);
                    Console.WriteLine("Reloaded state.");
                    return CrawlResult.Silent;

                #region Analysis
                case "au" or "union" when node is IArrayNode a:
                    AnalyzeUnion(data, a);
                    return CrawlResult.Navigate;
                case "af" or "analyze" when node is IArrayNode a:
                    var r = a.AnalyzeFields(data);
                    if (a.Entries[0] is FlatBufferNodeField first)
                        PrintFieldAnalysis(r, first, data);
                    else
                        PrintFieldAnalysis(r);
                    return CrawlResult.Silent;
                case "af" or "analyze" when node is FlatBufferNodeField f:
                    PrintFieldAnalysis(f.AnalyzeFields(data), f, data);
                    return CrawlResult.Silent;

                case "oof" when node is FlatBufferNodeField fn:
                    Console.WriteLine(fn.VTable.GetFieldOrder());
                    return CrawlResult.Silent;
                case "oofd" when node is FlatBufferNodeField fn:
                    Console.WriteLine(fn.VTable.GetFieldOrder(fn.DataTableOffset));
                    return CrawlResult.Silent;

                case "mfc" when node is IArrayNode an:
                    var (index, max) = an.GetMaxFieldCountIndex();
                    Console.WriteLine(max != 0
                        ? $"Max field count is {max} @ entry index {index}"
                        : "No nodes have a detectable field count.");
                    return CrawlResult.Silent;
                #endregion

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
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.ForegroundColor = ConsoleColor.White;
            return CrawlResult.Error;
        }
    }

    private static void PrintFieldAnalysis(FieldAnalysisResult result)
    {
        foreach (var (index, obs) in result.Fields.OrderBy(z => z.Key))
            Console.WriteLine($"[{index}] {obs.Summary()}");
    }

    private static void PrintFieldAnalysis(FieldAnalysisResult result, FlatBufferNodeField node, byte[] data)
    {
        foreach (var (index, obs) in result.Fields.OrderBy(z => z.Key))
            Console.WriteLine($"[{index}] {obs.Summary(node, index, data)}");
    }

    private static void AnalyzeUnion(byte[] data, IArrayNode array)
    {
        var result = array.AnalyzeUnion(data);
        var unique = result.UniqueTypeCodes;
        Console.WriteLine($"Unique Types: {string.Join(" ", unique.Select(x => x.ToString("X")))}");
        foreach (var type in unique)
        {
            var fieldCount = result.MaxFieldCount(type);
            var sameFieldCount = result.SameFieldCount(type);
            var indexes = result.GetIndexes(type);
            var quality = sameFieldCount ? "present in all" : fieldCount == 1 ? "optional" : "varied";

            Console.WriteLine($"Type {type:X} has {fieldCount} fields ({quality}), at indexes: {string.Join(" ", indexes)}");
        }
    }
}
