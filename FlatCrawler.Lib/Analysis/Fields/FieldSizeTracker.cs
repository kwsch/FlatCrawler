using System;

namespace FlatCrawler.Lib;

/// <summary>
/// Tracks metadata about the shape of a field across many node fields.
/// </summary>
/// <param name="Min"></param>
/// <param name="Max"></param>
/// <param name="IsUncertain"></param>
public sealed record FieldSizeTracker(int Min, int Max, bool IsUncertain)
{
    /// <summary> Minimum byte size of the field. </summary>
    public int Min { get; private set; } = Min;

    /// <summary> Maximum byte size of the field. </summary>
    public int Max { get; private set; } = Max;

    /// <summary> Indicates if the field size is not an exact value. </summary>
    public bool IsUncertain { get; private set; } = IsUncertain;

    /// <summary>
    /// Updates the size observations with another observed field size.
    /// </summary>
    public void Observe(int min, int max, bool isUncertain)
    {
        if (max != Max)
        {
            // The last field (highest offset) may have unused padding afterwards.
            // Only allow reducing the size of the field if it was the last field when first witnessed.
            if (!IsUncertain && !isUncertain)
                throw new InvalidOperationException("Field size mismatch");
        }

        Max = Math.Min(Max, max);
        Min = Math.Max(Min, min);
        IsUncertain &= isUncertain;
    }

    /// <summary>
    /// Gets a <see cref="FieldType"/> lumping group of what this field may represent based on all observed sizes.
    /// </summary>
    public FieldType GuessOverallType() => (Min, Max) switch
    {
        (<=4, >=4) => FieldType.All,
        (6, 6) => FieldType.StructInlined,
        (_, 1) => FieldType.StructSingle,
        (_, <4) => FieldType.StructValue,
        (>8, _) => FieldType.StructInlined,
        (>4, _) => FieldType.StructValue,
    };

    /// <summary>
    /// Checks if the <see cref="size"/> is within the observed range.
    /// </summary>
    public bool IsPlausible(int size) => size >= Min && size <= Max;

    public string Summary()
    {
        if (IsUncertain && Min != Max)
            return $"{Min}..{Max}?";
        return Max.ToString();
    }
}
