using System;
using System.Globalization;

namespace FlatCrawler.Lib;

public static class CommandUtil
{
    // TODO: Make a nice wrapper class for file data
    public static byte[] Data { get; set; } = Array.Empty<byte>();

    public static (int FieldIndex, string FieldType) GetDualArgs(string args)
    {
        var argSplit = args.Split(' ');
        return (int.Parse(argSplit[0]), argSplit[1]);
    }

    public static int GetIntPossibleHex(string txt) => txt.Contains("0x")
        ? int.Parse(txt.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
        : int.Parse(txt, CultureInfo.InvariantCulture);

    public static FlatBufferNode ReadNode(this FlatBufferNode node, int fieldIndex, string type, ReadOnlySpan<byte> data) => node switch
    {
        IArrayNode a => a.GetEntry(fieldIndex),
        FlatBufferNodeField r => r.ReadNodeAndTrack(fieldIndex, data, type),
        _ => throw new ArgumentException($"Node at {fieldIndex} has an unrecognized node type ({node.GetType().Name})."),
    };

    private static FlatBufferNode ReadNodeAndTrack(this FlatBufferNodeField node, int fieldIndex, ReadOnlySpan<byte> data, string type)
    {
        var code = TypeCodeUtil.GetTypeCode(type);
        bool asArray = type == "table" || type.Contains("[]");
        return node.ReadNodeAndTrack(fieldIndex, data, code, asArray);
    }

    public static FlatBufferNode ReadNodeAndTrack(this FlatBufferNodeField node, int fieldIndex, ReadOnlySpan<byte> data, TypeCode code, bool asArray)
    {
        var result = node.ReadNode(fieldIndex, data, code, asArray);
        node.TrackChildFieldNode(fieldIndex, code, asArray, result);
        return result;
    }
}
