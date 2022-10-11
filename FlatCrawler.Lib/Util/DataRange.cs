using System;

namespace FlatCrawler.Lib;

public record DataRange : IComparable<DataRange>
{
    public int Offset { get; init; }
    public int Length { get; init; }
    public bool IsSubRange { get; init; }
    public string Description { get; init; }

    public int Start => Offset;
    public int End => Offset + Length;

    public DataRange(int offset, int length, bool isSubRange = false, string desc = "")
    {
        Offset = offset;
        Length = length;
        IsSubRange = isSubRange;
        Description = desc;
    }

    public ReadOnlySpan<byte> ToSpan(ReadOnlySpan<byte> data)
    {
        return data[Start..End];
    }

    public override string ToString()
    {
        return $"[0x{Start:X}..0x{End:X}] (Length: {Length})";
    }

    public int CompareTo(DataRange? other)
    {
        int r = Offset.CompareTo(other?.Offset);

        if (r == 0)
            r = (other?.Length ?? 0).CompareTo(Length); // Bigger entry first

        if (r == 0)
            r = IsSubRange.CompareTo(other?.IsSubRange);

        return r;
    }
}

