using System;
using System.Collections.Generic;
using System.Linq;

namespace FlatCrawler.Lib;

/// <summary>
/// Stores results of a schema's field analysis to identify the possible types of each field.
/// </summary>
public sealed class FieldAnalysisResult
{
    /// <summary>
    /// Map of field index to metadata about the field.
    /// </summary>
    public readonly Dictionary<int, FieldObservations> Fields = new();

    /// <summary>
    /// Indicates if any field can be interpreted as any type. If false, the schema is invalid (not an object).
    /// </summary>
    public bool IsAnyFieldRecognized => Fields.Any(z => z.Value.Type.IsRecognized);

    /// <summary>
    /// First step: scan all fields to determine the minimum and maximum size of each field.
    /// </summary>
    public void ScanFieldSize(IEnumerable<FlatBufferNodeField> nodes)
    {
        foreach (var entry in nodes)
            ScanFieldSize(entry);
    }

    /// <summary>
    /// Second step: guess the overall type of the field based on the minimum and maximum size.
    /// </summary>
    public void GuessOverallType()
    {
        foreach (var (_, field) in Fields)
            field.Type.PreCheck(field.Size);
    }

    /// <summary>
    /// Third step: scan all nodes to determine the possible types of each field.
    /// </summary>
    public void ScanFieldType(IReadOnlyCollection<FlatBufferNodeField> nodes, ReadOnlySpan<byte> data)
    {
        foreach (var entry in nodes)
            ScanFieldType(entry, data);
    }

    private void ScanFieldSize(FlatBufferNodeField node)
    {
        // get the field indexes in ascending offset
        var presentFields = node.VTable.FieldInfo
            .Where(z => z.HasValue)
            .OrderBy(z => z.Offset)
            .ToArray();
        if (presentFields.Length == 0)
            return;

        // Serialized FlatBuffers store smallest sized fields first (increasing width).
        var order = node.FieldOrder = VTableFieldInfo.CheckFieldOrder(presentFields);
        if (order is FieldOrder.IncreasingSize or FieldOrder.Unchecked)
            CheckSizeIncreasing(presentFields);
        else if (order is FieldOrder.DecreasingSize)
            CheckSizeDecreasing(presentFields);
        else
            CheckSizeMixed(presentFields);
    }

    private void CheckSizeIncreasing(VTableFieldInfo[] presentFields)
    {
        // Since we know the size of each field increases from left to right,
        // we can use the offset of the next field to check if our size is exact.
        int min = 1;
        foreach ((int index, _, int maxSize) in presentFields)
        {
            bool isUncertain = !GetIsSizeExactIncreasing(presentFields, index);
            // Scan the field size and double check results from prior calls.
            if (!Fields.TryGetValue(index, out var check))
                Fields.Add(index, new(new(min, maxSize, isUncertain)));
            else
                check.Size.Observe(min, maxSize, isUncertain);

            // Ascending size, so the next iteration will be at least this size.
            min = maxSize;
        }
    }

    private void CheckSizeDecreasing(VTableFieldInfo[] presentFields)
    {
        foreach (var field in presentFields)
        {
            var index = field.Index;
            (int min, int max) = GetIsSizeExactDecreasing(presentFields, index);
            bool isUncertain = min != max;
            // Scan the field size and double check results from prior calls.
            if (!Fields.TryGetValue(index, out var check))
                Fields.Add(index, new(new(min, max, isUncertain)));
            else
                check.Size.Observe(min, max, isUncertain);
        }
    }

    private void CheckSizeMixed(VTableFieldInfo[] presentFields)
    {
        foreach (var field in presentFields)
        {
            var index = field.Index;
            (int min, int max) = GetIsSizeExactDecreasing(presentFields, index);
            bool isUncertain = min != max;
            // Scan the field size and double check results from prior calls.
            if (!Fields.TryGetValue(index, out var check))
                Fields.Add(index, new(new(min, max, isUncertain)));
            else
                check.Size.Observe(min, max, isUncertain);
        }
    }

    private static bool GetIsSizeExactIncreasing(VTableFieldInfo[] presentFields, int fieldIndex)
    {
        var index = Array.FindIndex(presentFields, z => z.Index == fieldIndex);
        if (index == -1)
            throw new InvalidOperationException("Field not found in VTable.");

        var info = presentFields[index];
        if ((info.Offset & 1) != 0)
            return true; // odd offset is always certain (byte)

        if (index == presentFields.Length - 1)
            return false; // last field is always uncertain

        if (index == 0)
            return false; // can't tell if u16 -> u32 due to padding

        var prev = presentFields[index - 1];
        return prev.Size == info.Size;
    }

    private static (int Min, int Max) GetIsSizeExactDecreasing(VTableFieldInfo[] presentFields, int fieldIndex)
    {
        var index = Array.FindIndex(presentFields, z => z.Index == fieldIndex);
        if (index == -1)
            throw new InvalidOperationException("Field not found in VTable.");

        var info = presentFields[index];
        if ((info.Offset & 1) != 0)
            return (1, 1); // odd offset is always certain (byte)

        if (index == presentFields.Length - 1)
        {
            if (presentFields.Length == 1)
                return (info.Size, info.Size); // only field is always certain
            var prev = presentFields[index - 1];
            return (1, Math.Min(prev.Size, info.Size)); // last field is always uncertain
        }

        var next = presentFields[index + 1];
        if (next.Size == info.Size)
            return (info.Size, info.Size); // next field is same size, so this field is certain
        return (next.Size, info.Size);
    }

    private void ScanFieldType(FlatBufferNodeField entry, ReadOnlySpan<byte> data)
    {
        foreach (var (index, field) in Fields)
            field.Observe(entry, index, data);
    }
}
