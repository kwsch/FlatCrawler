using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FlatCrawler.Lib;

/// <summary>
/// Draft file object
/// </summary>
public sealed class FlatBufferFile
{
    // Data should never overlap with any of the data ranges in this set
    public readonly SortedSet<DataRange> ProtectedDataRanges = new();

    private readonly SortedDictionary<int, VTable> VTables = new();

    private readonly ReadOnlyMemory<byte> _data;
    public ReadOnlySpan<byte> Data => _data.Span;

    public bool IsValid => GetIsSizeValid(Data);

    public FlatBufferFile(string path) : this(File.ReadAllBytes(path).AsMemory()) { }
    public FlatBufferFile(byte[] data) : this(data.AsMemory()) { }
    public FlatBufferFile(ReadOnlyMemory<byte> data) => _data = data;

    public void SetProtectedMemory(DataRange range)
    {
        EnsureNoAccessViolation(range);

        DataRange[] overlappingPadding = ProtectedDataRanges.Where(r => r.Category == DataCategory.Padding && r.IsOverlapping(range)).ToArray();

        foreach (var padding in overlappingPadding)
        {
            // If the new range contains the padding in full, we need to remove it
            // Otherwise the padding will need to be adjusted to the new range.
            ProtectedDataRanges.Remove(padding);

            if (range.Contains(padding))
                continue;


            if (padding.Start < range.End)
            {
                // The padding is attached to the end if the range
                ProtectedDataRanges.Add(padding with { Range = range.End..padding.End });
            }
            else
            {
                // The padding is attached to the start if the range
                ProtectedDataRanges.Add(padding with { Range = padding.Start..range.Start });
            }
        }

        ProtectedDataRanges.Add(range);
    }

    public void RemoveProtectedMemory(DataRange range)
    {
        ProtectedDataRanges.Remove(range);
    }

    /// <summary>
    /// Check if any of the protected ranges overlap with the provided range
    /// </summary>
    /// <param name="input">The range to check</param>
    /// <exception cref="AccessViolationException">When the range overlaps protected memory</exception>
    public void EnsureNoAccessViolation(DataRange input)
    {
        var overlappingRange = FindFirstOverlapping(input);
        if (overlappingRange != default)
            throw new AccessViolationException($"Data range {input} ({input.Description}), would overlap protected memory at: {overlappingRange} ({overlappingRange.Description})");
    }

    public bool IsAccessViolation(DataRange range) => FindFirstOverlapping(range) != default;

    private DataRange FindFirstOverlapping(DataRange input)
    {
        if (ShouldIgnoreOverlap(input))
            return default;

        foreach (var exist in ProtectedDataRanges)
        {
            if (!ShouldIgnoreOverlap(exist) && input.IsOverlapping(exist))
                return exist;
        }
        return default;
    }

    /// <summary>
    /// Check if the range should be tested for potential overlap
    /// </summary>
    private static bool ShouldIgnoreOverlap(DataRange range) => range.IsSubRange || range.Category == DataCategory.Padding;

    /// <summary>
    /// Read the VTable at <paramref name="offset"/>.
    /// If a VTable was already registered at that location, the registered VTable will be returned.
    /// </summary>
    /// <param name="offset">The offset of the VTable from the start of the file in bytes.</param>
    /// <returns>The VTable at <paramref name="offset"/></returns>
    public VTable PeekVTable(int offset)
    {
        if (VTables.TryGetValue(offset, out VTable? table))
            return table;
        return new(this, offset);
    }

    public void RegisterVTable(VTable vTable)
    {
        vTable.RefCount++;

        if (vTable.RefCount != 1)
            return;

        VTables.Add(vTable.Location, vTable);
        SetProtectedMemory(vTable.VTableMemory);

        // Check for alignment padding
        var alignedAddress = (int)MemoryUtil.BackwardAlignToBytes((uint)vTable.Location, 4);
        if (alignedAddress == vTable.Location)
            return;

        var alignmentPadding = new DataRange(alignedAddress..vTable.Location, DataCategory.Padding, "Alignment Padding");
        SetProtectedMemory(alignmentPadding);
    }

    public void UnRegisterVTable(VTable vTable)
    {
        vTable.RefCount--;
        Debug.Assert(vTable.RefCount >= 0, "VTable ref count is below zero!");

        if (vTable.RefCount != 0)
            return;

        VTables.Remove(vTable.Location);
        RemoveProtectedMemory(vTable.VTableMemory);

        // Remove alignment padding if it was applied
        var alignedAddress = (int)MemoryUtil.BackwardAlignToBytes((uint)vTable.Location, 4);
        if (alignedAddress == vTable.Location)
            return;

        var alignmentPadding = new DataRange(alignedAddress..vTable.Location, DataCategory.Padding, "Alignment Padding");
        RemoveProtectedMemory(alignmentPadding);
    }

    public static bool GetIsSizeValid(ReadOnlySpan<byte> data) => data.Length >= 8;
}
