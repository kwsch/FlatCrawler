using System;

namespace FlatCrawler.Lib;

public readonly record struct DataRange(Range Range, bool IsSubRange = false, string Description = "")
{
    public int Offset => Range.Start.Value;
    public int Length => Range.End.Value - Range.Start.Value;

    public override string ToString()
    {
        return $"[0x{Offset:X}..0x{Range.End.Value:X}] (Length: {Length})";
    }
}
