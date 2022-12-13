using System;

namespace FlatCrawler.Lib;

public enum DataCategory
{
    None,
    Value,
    DataTable,
    VTable,
    Pointer,
    Padding,
    Misc,
}

public readonly record struct DataRange(Range Range, DataCategory Category, string Description = "", bool IsSubRange = false) : IComparable<DataRange>
{
    public int Start => Range.Start.Value;
    public int End => Range.End.Value;

    public int Offset => Range.Start.Value;
    public int Length => Range.End.Value - Range.Start.Value;

    public override string ToString()
    {
        return $"[0x{Offset:X}..0x{Range.End.Value:X}] (Length: {Length,3})";
    }

    public int CompareTo(DataRange other)
    {
        int r = Offset.CompareTo(other.Offset);

        if (r == 0)
            r = Category.CompareTo(other.Category);

        if (r == 0)
            r = Length.CompareTo(other.Length);

        return r;
    }

    public bool IsOverlapping(DataRange other) => other.End > Start && other.Start < End;
    public bool Contains(DataRange other) => Start >= other.Start && End <= other.End;
}
