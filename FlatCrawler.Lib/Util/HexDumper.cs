using System;
using System.Text;

// Content here is licensed CC BY-SA 3.0 instead of the repository's license.

namespace FlatCrawler.Lib;

/// <summary>
/// Modified from https://codereview.stackexchange.com/q/145506
/// </summary>
public ref struct HexDumper(ReadOnlySpan<byte> data, int absoluteOffset, HexDumperConfig config)
{
    private readonly ReadOnlySpan<byte> Data = data; // relative span
    private readonly HexDumperConfig Config = config;
    private readonly int AbsoluteOffset = absoluteOffset; // offset Data originates from

    private int _index = 0; // bytes dumped so far

    public static string Dump(ReadOnlySpan<byte> data, int absoluteOffset)
    {
        var span = data[..Math.Min(data.Length, 0x80)];
        var config = new HexDumperConfig();
        var dumper = new HexDumper(span, absoluteOffset, config);
        var sb = new StringBuilder();
        dumper.Dump(sb);
        return sb.ToString();
    }

    public void Dump(StringBuilder sb)
    {
        if (Config.ShowHeader)
            WriteHeader(sb);
        WriteBody(sb);
    }

    private readonly void WriteHeader(StringBuilder sb)
    {
        const string ofs = "Offset(h)| ";

        for (int i = 0; i < (Config.BytesPerLine * 3) + ofs.Length - 1; i++)
            sb.Append('-');
        sb.AppendLine();

        if (Config.ShowOffset)
            sb.Append(ofs);

        for (int i = 0; i < Config.BytesPerLine; i++)
        {
            sb.AppendFormat("{0:X2}", i & 0xFF);
            if (i + 1 < Config.BytesPerLine)
                sb.Append(' ');
        }
        sb.AppendLine();

        for (int i = 0; i < (Config.BytesPerLine * 3) + ofs.Length - 1; i++)
            sb.Append('-');
        sb.AppendLine();
    }

    private void WriteBody(StringBuilder sb)
    {
        int ctr;
        while ((ctr = _index) < Data.Length)
        {
            if (ctr % Config.BytesPerLine == 0)
            {
                if (ctr > 0)
                {
                    if (Config.ShowAscii)
                        WriteAscii(sb);
                    sb.AppendLine();
                }

                if (Config.ShowOffset)
                    WriteOffset(sb);
            }

            WriteByte(sb);
            if ((ctr = _index) % Config.BytesPerLine != 0 && ctr < Data.Length)
                sb.Append(' ');
        }

        if (Config.ShowAscii)
            WriteAscii(sb);
    }

    private readonly void WriteOffset(StringBuilder sb)
    {
        sb.AppendFormat("{0:X8}", AbsoluteOffset + _index).Append(" | ");
    }

    private void WriteByte(StringBuilder sb)
    {
        sb.AppendFormat("{0:X2}", Data[_index]);
        _index++;
    }

    private readonly void WriteAscii(StringBuilder sb)
    {
        var ctr = _index;
        int backtrack = ((ctr - 1) / Config.BytesPerLine) * Config.BytesPerLine;
        int length = ctr - backtrack;

        // This is to fill up last string of the dump if it's shorter than Config.BytesPerLine
        sb.Append(new string(' ', (Config.BytesPerLine - length) * 3));

        sb.Append("   ");
        sb.EnsureCapacity(sb.Length + length);

        var span = Data.Slice(backtrack, length);
        foreach (var b in span)
        {
            char value = b is < 0x20 or > 0x7E ? '.' : (char)b;
            sb.Append(value);
        }
    }
}

public readonly record struct HexDumperConfig
{
    public int BytesPerLine { get; init; } = 0x10;
    public bool ShowHeader { get; init; } = true;
    public bool ShowOffset { get; init; } = true;
    public bool ShowAscii { get; init; } = true;
    public HexDumperConfig() { }
}
