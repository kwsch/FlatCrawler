using System;
using System.Globalization;

namespace FlatCrawler.Lib;

public static class CommandUtil
{
    /// <summary>
    /// Gets an integer-string tuple from the command line arguments.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <exception cref="ArgumentException">Thrown if the arguments are invalid.</exception>
    public static (int Index, string String) GetDualArgs(ReadOnlySpan<char> args)
    {
        args = args.Trim();
        var space = args.IndexOf(' ');
        if (space == -1)
            throw new ArgumentException("Expected two arguments, got one", nameof(args));

        var num = int.Parse(args[..space]);
        var second = args[(space + 1)..];
        space = second.IndexOf(' ');
        if (space != -1)
            second = second[..space];
        return (num, second.Trim().ToString());
    }

    private const string HexPrefix = "0x";

    public static int GetIntFromHex(ReadOnlySpan<char> hex)
    {
        hex = hex.Trim();
        if (hex.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase))
            hex = hex[HexPrefix.Length..];
        return int.Parse(hex, NumberStyles.HexNumber);
    }

    public static int GetIntPossibleHex(ReadOnlySpan<char> txt)
    {
        txt = txt.Trim();
        return txt.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase)
            ? int.Parse(txt[HexPrefix.Length..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : int.Parse(txt, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    public static FlatBufferNode ReadNode(this FlatBufferNode node, int fieldIndex, ReadOnlySpan<byte> data, ReadOnlySpan<char> type) => node switch
    {
        IArrayNode a => a.GetEntry(fieldIndex),
        FlatBufferNodeField r => r.ReadNodeAndTrack(fieldIndex, data, type),
        _ => throw new ArgumentException($"Node at {fieldIndex} has an unrecognized node type ({node.GetType().Name})."),
    };

    private static FlatBufferNode ReadNodeAndTrack(this FlatBufferNodeField node, int fieldIndex, ReadOnlySpan<byte> data, ReadOnlySpan<char> type)
    {
        (bool asArray, TypeCode code) = TypeCodeUtil.GetTypeCodeTuple(type);
        return node.ReadNodeAndTrack(fieldIndex, data, code, asArray);
    }

    public static FlatBufferNode ReadNodeAndTrack(this FlatBufferNodeField node, int fieldIndex, ReadOnlySpan<byte> data, TypeCode code, bool asArray)
    {
        var result = node.ReadNode(fieldIndex, data, code, asArray);
        node.TrackChildFieldNode(fieldIndex, data, code, asArray, result);
        return result;
    }
}
