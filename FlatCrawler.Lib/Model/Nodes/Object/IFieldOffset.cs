using System.Linq;

namespace FlatCrawler.Lib;

/// <summary>
/// Exposes a relative offset for where the data exists for the field.
/// </summary>
public interface IFieldOffset
{
    public int Offset { get; }

    /// <summary>
    /// The field has a value stored in the buffer if the offset is greater than 0.
    /// Zero valued offsets are used to indicate that the field is not present.
    /// </summary>
    public bool HasValue { get; }
}

public readonly record struct FieldOffsetIndex(int Offset, int Index) : IFieldOffset
{
    public bool HasValue => Offset != 0;
}

public static class FieldOffsetExtensions
{
    public static FieldOffsetIndex[] GetOrderedList<T>(this T[] array) where T : IFieldOffset
    {
        return array.Select((x, Index) => new FieldOffsetIndex(x.Offset, Index))
            .Where(x => x.HasValue) // Zero offsets don't exist in the table so have no size
            .OrderByDescending(z => z.Offset)
            .ToArray();
    }
}
