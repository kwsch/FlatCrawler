using System;

namespace FlatCrawler.Lib;

public record FieldSizeTracker(int Min, int Max, bool IsUncertain)
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

    public FieldType GuessOverallType()
    {
        if (Min == 4 && !IsUncertain)
            return FieldType.All;
        if (Min == 6 && Max == 6)
            return FieldType.StructInlined;

        if (Max == 1)
            return FieldType.StructSingle;
        if (Max < 4)
            return FieldType.StructValue;
        if (Min > 8)
            return FieldType.StructInlined;
        if (Min > 4)
            return FieldType.StructValue;

        return FieldType.StructValue;
    }

    public bool IsPlausible(int size) => size >= Min && size <= Max;

    public string Summary()
    {
        if (IsUncertain && Min != Max)
            return $"{Min}..{Max}?";
        return Max.ToString();
    }
}
