using System.Collections.Generic;

namespace FlatCrawler.Lib;

/// <summary>
/// Represents a field pointer within a <see cref="VTable"/>.
/// </summary>
/// <param name="Index">Field Index</param>
/// <param name="Offset">Relative offset to where the field's serialized value is stored.</param>
/// <param name="Size">
/// How many bytes are allocated to store the field's serialized value.
/// NOTE: this is derived on object construction based on the amount of data remaining, and isn't precise.
/// </param>
public sealed record VTableFieldInfo(int Index, int Offset, int Size) : IFieldOffset
{
    public bool HasValue => Offset != 0;

    public static FieldOrder CheckFieldOrder(IReadOnlyList<VTableFieldInfo> ascendingOffset)
    {
        if (ascendingOffset.Count <= 1)
            return FieldOrder.Unchecked;
        if (GetIsIncreasingSize(ascendingOffset))
            return FieldOrder.IncreasingSize;
        if (GetIsDecreasingSize(ascendingOffset))
            return FieldOrder.DecreasingSize;
        return FieldOrder.Mixed;
    }

    private static bool GetIsIncreasingSize(IReadOnlyList<VTableFieldInfo> ascendingOffset)
    {
        int size = ascendingOffset[0].Size;
        for (int i = 1; i < ascendingOffset.Count; i++)
        {
            var field = ascendingOffset[i];
            if (field.Size < size)
                return false;
            size = field.Size;
        }

        return true;
    }

    private static bool GetIsDecreasingSize(IReadOnlyList<VTableFieldInfo> ascendingOffset)
    {
        int size = ascendingOffset[0].Size;
        for (int i = 1; i < ascendingOffset.Count; i++)
        {
            var field = ascendingOffset[i];
            if (field.Size > size)
                return i != ascendingOffset.Count - 1;
            size = field.Size;
        }
        return true;
    }
}
