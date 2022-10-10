using System;
using System.Collections.Generic;

namespace FlatCrawler.Lib;

public sealed class FlatBufferPrinter
{
    private readonly List<string> Output = new();

    public int MaxPrefixTable { get; init; } = 7;
    public int MaxSuffixTable { get; init; } = 3;
    public string Unexplored { get; init; } = "???";
    public string Default { get; init; } = "Default";
    public string LinkedNode { get; init; } = "<---";
    public string RootName { get; init; } = "Root";
    public int MaxLengthBeginTrim { get; set; } = 20;

    public IEnumerable<string> GeneratePrint(FlatBufferNode node)
    {
        Output.Clear();
        PrintLocation(node);
        return Output;
    }

    private void PrintLocation(FlatBufferNode node)
    {
        Output.Add(node is FlatBufferRoot ? $"{RootName} {LinkedNode}" : RootName);
        var ll = new LinkedList<FlatBufferNode>();
        ll.AddFirst(node);
        var parent = node.Parent;
        while (parent is { })
        {
            ll.AddFirst(parent);
            parent = parent.Parent;
        }

        var lines = BuildTree(ll.First!);
        Output.AddRange(lines);
    }

    private List<string> BuildTree(LinkedListNode<FlatBufferNode> linkNode)
    {
        List<string> result = new();
        AppendNodeData(linkNode, result, 0);
        return result;
    }

    private void AppendNodeData(LinkedListNode<FlatBufferNode> linkNode, List<string> result, int depth)
    {
        var node = linkNode.Value;
        var child = linkNode.Next;
        switch (node)
        {
            case FlatBufferNodeField f:
                AppendFieldNodes(result, depth, f, child);
                break;
            case IArrayNode a:
                AppendArrayNodes(result, depth, node, child, a);
                break;
            default:
                result.Add(GetDepthPadded(node.FullNodeName, depth));
                break;
        }
    }

    private void AppendArrayNodes(List<string> result, int depth, FlatBufferNode node, LinkedListNode<FlatBufferNode>? child, IArrayNode a)
    {
        var cn = child?.Value;
        var iterMid = node.GetChildIndex(cn);
        var x = a.Entries;
        if (cn is null)
            iterMid = a.Entries.Count - 1;
        var iter = Math.Min(MaxPrefixTable, iterMid);

        for (int i = 0; i <= iter; i++)
            result.Add(GetDepthPadded($"[{i}] {GetNodeDescription(node, x, i, cn)}", depth));

        if (iterMid != iter && cn is not null)
            result.Add(GetDepthPadded($"[{iterMid}] {GetNodeDescription(node, x, iterMid, cn)}", depth));

        if (cn is null)
            return;

        AppendNodeData(child!, result, depth + 1);

        if (result.Count > MaxLengthBeginTrim)
        {
            result.Add(GetDepthPadded("...", depth));
            return;
        }

        var resume = Math.Max(a.Entries.Count - MaxSuffixTable - 1, iterMid + 1);
        for (int i = resume; i < a.Entries.Count; i++)
            result.Add(GetDepthPadded($"[{i}] {GetNodeDescription(node, x, i, cn)}", depth));
    }

    private void AppendFieldNodes(List<string> result, int depth, FlatBufferNodeField node, LinkedListNode<FlatBufferNode>? child)
    {
        var cn = child?.Value;
        var iterMid = node.GetChildIndex(cn);
        var x = node.AllFields;
        if (cn is null)
            iterMid = x.Count - 1;

        for (int i = 0; i <= iterMid; i++)
            result.Add(GetDepthPadded($"[{i}] {GetNodeDescription(node, x, i, cn)}", depth));

        if (cn is null)
            return;

        AppendNodeData(child!, result, depth + 1);

        if (result.Count > MaxLengthBeginTrim)
        {
            result.Add(GetDepthPadded("...", depth));
            return;
        }

        for (int i = iterMid + 1; i < x.Count; i++)
            result.Add(GetDepthPadded($"[{i}] {GetNodeDescription(node, x, i, cn)}", depth));
    }

    private string GetNodeDescription(FlatBufferNode parent, IReadOnlyList<FlatBufferNode?> x, int index, FlatBufferNode? cmp)
    {
        if (parent is FlatBufferNodeField f && !f.HasField(index))
            return Default;
        var entry = x[index];
        if (entry is null)
            return GetUnexplored(parent, index);
        if (cmp is not null && ReferenceEquals(cmp, entry))
            return $"{cmp.FullNodeName} {LinkedNode}";
        return entry.FullNodeName;
    }

    private string GetUnexplored(FlatBufferNode parent, int index)
    {
        if (parent is not FlatBufferNodeField f)
            return Unexplored;

        var fi = f.VTable.FieldInfo[index];
        return $"{Unexplored} [{fi.Size}]";
    }

    public static string GetDepthPadded(string str, int depth) => str.PadLeft(str.Length + (depth * 2), ' ');
}
