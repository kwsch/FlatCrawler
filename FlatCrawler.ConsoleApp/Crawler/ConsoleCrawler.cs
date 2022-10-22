using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FlatCrawler.Lib;

namespace FlatCrawler.ConsoleApp;

public sealed class ConsoleCrawler
{
    private readonly List<string> ProcessedCommands = new();
    private string SaveStatePath = "lines.txt";

    private readonly string FilePath;

    private readonly FlatBufferFile FbFile;

    public ConsoleCrawler(string path, FlatBufferFile file)
    {
        FilePath = path;
        FbFile = file;
    }

    public void CrawlLoop()
    {
        var fn = Path.GetFileName(FilePath);

        SaveStatePath = Path.ChangeExtension(fn, ".txt");

        Console.WriteLine($"Crawling {Console.Title = fn}...");
        Console.WriteLine();

        FlatBufferNode node = FlatBufferRoot.Read(FbFile, 0);
        node.PrintTree();

        Console.OutputEncoding = Encoding.UTF8; // japanese strings will show up as boxes rather than ????
        while (true)
        {
            Console.Write(">>> ");
            var cmd = Console.ReadLine();
            if (cmd is null)
                break;
            var result = ProcessCommand(cmd, ref node, FbFile.Data);
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

    private static FlatBufferNode? GetNodeAtIndex(FlatBufferNode parent, int index) => parent switch
    {
        IFieldNode fn => fn.GetField(index) ?? throw new ArgumentNullException(nameof(FlatBufferNode), "node not explored yet."),
        IArrayNode an => an.GetEntry(index),
        _ => null,
    };

    private const int CommandMaxLength = 20;

    private CrawlResult ProcessCommand(ReadOnlySpan<char> cmd, ref FlatBufferNode node, ReadOnlySpan<byte> data)
    {
        cmd = cmd.Trim();
        if (cmd.Length == 0)
            return CrawlResult.Unrecognized;

        var indexOfFirstSpace = cmd.IndexOf(' ');
        if (indexOfFirstSpace == -1)
        {
            if (cmd.Length > CommandMaxLength)
                return CrawlResult.Unrecognized;

            Span<char> lower = stackalloc char[cmd.Length];
            cmd.ToLowerInvariant(lower);
            return ProcessCommandSingle(lower, ref node, data);
        }

        if (indexOfFirstSpace > CommandMaxLength)
            return CrawlResult.Unrecognized;

        var args = cmd[(indexOfFirstSpace + 1)..];
        var cmdCased = cmd[..indexOfFirstSpace];
        Span<char> cmdLower = stackalloc char[cmdCased.Length];
        cmdCased.ToLowerInvariant(cmdLower);

        try
        {
            return ProcessCommand(cmdLower, args, ref node, data);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.ResetColor();
            return CrawlResult.Error;
        }
    }

    private static CrawlResult ProcessCommand(ReadOnlySpan<char> cmd, ReadOnlySpan<char> args, ref FlatBufferNode node, ReadOnlySpan<byte> data)
    {
        switch (cmd)
        {
            case "n" or "name" or "fieldname":
            {
                if (!args.Contains(' '))
                {
                    node.Name = args.ToString();
                    return CrawlResult.Update;
                }

                var argSplit = CommandUtil.GetDualArgs(args);
                var toRename = GetNodeAtIndex(node, argSplit.Index);
                if (toRename != null)
                    toRename.Name = argSplit.String;

                return CrawlResult.Update;
            }
            case "t" or "type" or "typename":
            {
                if (!args.Contains(' '))
                {
                    node.TypeName = args.ToString();
                    return CrawlResult.Update;
                }

                var argSplit = CommandUtil.GetDualArgs(args);
                var toRename = GetNodeAtIndex(node, argSplit.Index);
                if (toRename != null)
                    toRename.TypeName = argSplit.String;

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
                    node = p.GetField(index) ??
                           throw new ArgumentNullException(nameof(FlatBufferNode), "node not explored yet.");
                    return CrawlResult.Navigate;
                }

                var (fieldIndex, fieldType) = CommandUtil.GetDualArgs(args);
                var result = node.ReadNode(fieldIndex, data, fieldType.ToLowerInvariant());
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
                var result = node.ReadNode(fieldIndex, data, fieldType.ToLowerInvariant());
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
                    var y = x.ReadNode(objectIndex, data, "object");
                    var fc = ((FlatBufferNodeField)y).HasField(fieldIndex);
                    if (!fc)
                        continue;
                    Console.WriteLine(
                        $"Entry {i} has an object at field {objectIndex} with a value for its Field {fieldIndex}");
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
                var offset = CommandUtil.GetIntFromHex(args);
                var index = f.VTable.GetFieldIndex(offset - f.DataTableOffset);
                Console.WriteLine($"Offset {offset:X} is Field {index}");
                return CrawlResult.Silent;
            }

            case "hex" or "h":
            {
                var offset = CommandUtil.GetIntFromHex(args);
                DumpHex(data, offset);
                return CrawlResult.Silent;
            }

            case "fnv" or "hash":
            {
                var hashCode = FnvHash.HashFnv1a_64(args);
                Console.WriteLine($"Fnv1a_64 hash of {args} is: 0x{hashCode:X} (Uint64 {hashCode})");
                return CrawlResult.Silent;
            }

            default:
                return CrawlResult.Unrecognized;
        }
    }

    private static void DumpHex(ReadOnlySpan<byte> data, int absoluteOffset)
    {
        string dump = HexDumper.Dump(data[absoluteOffset..], absoluteOffset);
        Console.WriteLine(dump);
    }

    private CrawlResult ProcessCommandSingle(ReadOnlySpan<char> cmd, ref FlatBufferNode node, ReadOnlySpan<byte> data)
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
                case "save" or "dump":
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

                case "v":
                    PrintProtectedRanges();
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
            Console.ResetColor();
            return CrawlResult.Error;
        }
    }

    private void PrintProtectedRanges()
    {
        bool IsAlignmentPadding(DataRange missingRange, int lastRangeEnd)
        {
            // If it's 2 bytes, it's probably padding. Check alignment to 4 bytes.
            // TODO: Should probably also validate that the range is actually for a data table.
            // TODO: It seems VTables get right-aligned (left padded) to the nearest 4 byte boundry

            if (missingRange.Length == 2 && !MemoryUtil.IsAligned((uint)lastRangeEnd, 4))
            {
                Console.WriteLine($"Found '{"Alignment Padding",-26}' at Range: {missingRange}");
                return true;
            }

            return false;
        }

        bool DisplaySubRanges = true; // TODO: GLOBAL SETTING?
        int totalBytes = FbFile.Data.Length;
        int dataSize = FbFile.Data.Length;

        var missingDataRanges = new List<DataRange>();
        int lastRangeEnd = 0;
        foreach (var range in FbFile.ProtectedDataRanges)
        {
            if (range.IsSubRange && !DisplaySubRanges)
                continue;

            if (!range.IsSubRange)
            {
                if (lastRangeEnd != range.Start)
                {
                    var missingRange = new DataRange(lastRangeEnd..range.Start);

                    if (IsAlignmentPadding(missingRange, lastRangeEnd))
                        dataSize -= missingRange.Length;
                    else
                        missingDataRanges.Add(missingRange);
                }

                dataSize -= range.Length;
                lastRangeEnd = range.End;
            }

            var str = $"Found '{range.Description,-26}' at Range: {range}";
            Console.WriteLine(FlatBufferPrinter.GetDepthPadded(str, range.IsSubRange ? 1 : 0));
        }

        if (lastRangeEnd != totalBytes)
        {
            var missingRange = new DataRange(lastRangeEnd..totalBytes);
            if (IsAlignmentPadding(missingRange, lastRangeEnd))
                dataSize -= missingRange.Length;
            else
                missingDataRanges.Add(missingRange);
        }

        Console.WriteLine($"Total data size: {totalBytes} bytes");
        Console.WriteLine($"Data accounted for: {totalBytes - dataSize} bytes (unaccounted: {dataSize} bytes)");

        foreach (var missingRange in missingDataRanges)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Found unknown data at Range: {missingRange}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    private static void PrintFieldAnalysis(FieldAnalysisResult result)
    {
        foreach (var (index, obs) in result.Fields.OrderBy(z => z.Key))
            Console.WriteLine($"[{index}] {obs.Summary()}");
    }

    private static void PrintFieldAnalysis(FieldAnalysisResult result, FlatBufferNodeField node, ReadOnlySpan<byte> data)
    {
        int hash = 0;
        var settings = new SchemaAnalysisSettings();
        FileAnalysis.RecursiveDump(node, data, result.Fields, Console.Out, ref hash, settings);
    }

    private static void AnalyzeUnion(ReadOnlySpan<byte> data, IArrayNode array)
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
