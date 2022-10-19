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
        System.Diagnostics.Debug.Assert(GetIsIncreasingSize(presentFields));

        int min = 1;
        foreach (var field in presentFields)
        {
            var i = field.Index;
            var max = field.Size;

            bool isUncertain = !GetIsSizeExact(presentFields, i);
            // Scan the field size and double check results from prior calls.
            if (!Fields.TryGetValue(i, out var check))
                Fields.Add(i, new(new(min, max, isUncertain)));
            else
                check.Size.Observe(min, max, isUncertain);

            // Ascending size, so the next iteration will be at least this size.
            min = max;
        }
    }

    private static bool GetIsSizeExact(VTableFieldInfo[] presentFields, int i)
    {
        var index = Array.FindIndex(presentFields, z => z.Index == i);
        if (index == -1)
            throw new InvalidOperationException("Field not found in VTable.");

        if (index == presentFields.Length - 1)
            return false; // last field is always uncertain

        var info = presentFields[index];
        if ((info.Offset & 1) != 0)
            return true; // odd offset is always certain (byte)

        if (index == 0)
            return false; // can't tell if u16 -> u32 due to padding

        var prev = presentFields[index - 1];
        return prev.Size == info.Size;
    }

    private static bool GetIsIncreasingSize(IEnumerable<VTableFieldInfo> presentFieldsAscendingOffset)
    {
        int size = 0;
        foreach (var field in presentFieldsAscendingOffset)
        {
            if (field.Size < size)
                return false;
            size = field.Size;
        }
        return true;
    }

    private void ScanFieldType(FlatBufferNodeField entry, ReadOnlySpan<byte> data)
    {
        foreach (var (index, field) in Fields)
            field.Observe(entry, index, data);
    }
}
