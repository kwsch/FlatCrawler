using System;

namespace FlatCrawler.Lib;

public readonly record struct DataRange(Range Range, string Description = "", bool IsSubRange = false) : IComparable<DataRange>
{
    public int Start => Range.Start.Value;
    public int End => Range.End.Value;

    public int Offset => Range.Start.Value;
    public int Length => Range.End.Value - Range.Start.Value;

    public override string ToString()
    {
        return $"[0x{Offset:X}..0x{Range.End.Value:X}] (Length: {Length})";
    }

    public int CompareTo(DataRange other)
    {
        int r = Offset.CompareTo(other.Offset);

        if (r == 0)
            r = IsSubRange.CompareTo(other.IsSubRange);

        if (r == 0)
            r = Length.CompareTo(other.Length);

        return r;
    }
}
