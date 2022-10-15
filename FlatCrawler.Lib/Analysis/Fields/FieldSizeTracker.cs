using System;

namespace FlatCrawler.Lib;

public sealed record FieldSizeTracker(int Min, int Max, bool IsUncertain)
{
    public int Min { get; private set; } = Min;
    public int Max { get; private set; } = Max;
    public bool IsUncertain { get; private set; } = IsUncertain;

    public void Observe(int min, int max, bool isUncertain)
    {
        if (max != Max)
        {
            // The last field (highest offset) may have unused padding afterwards.
            // Only allow reducing the size of the field if it was the last field when first witnessed.
            if (!IsUncertain || max >= Max || min > Min)
                throw new InvalidOperationException("Field size mismatch");
        }

        Max = max; // same as Math.Min per logic above
        Min = Math.Max(Min, min);
        IsUncertain &= isUncertain;
    }

    public FieldType GuessOverallType() => (Min, Max) switch
    {
        (<=4, >=4) => FieldType.All,
        (6, 6) => FieldType.StructInlined,
        (_, 1) => FieldType.StructSingle,
        (_, <4) => FieldType.StructValue,
        (>8, _) => FieldType.StructInlined,
        (>4, _) => FieldType.StructValue,
    };

    public bool IsPlausible(int size) => size >= Min && size <= Max;

    public string Summary()
    {
        if (IsUncertain && Min != Max)
            return $"{Min}..{Max}?";
        return Max.ToString();
    }
}
