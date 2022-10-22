using System;
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

    public FlatBufferFile(string path) : this(File.ReadAllBytes(path)) { }
    public FlatBufferFile(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray();
    }

    public void SetProtectedMemory(DataRange range)
    {
        CheckAccessViolation(range);
        ProtectedDataRanges.Add(range);
    }

    public void RemoveProtectedMemory(DataRange range)
    {
        ProtectedDataRanges.Remove(range);
    }

    /// <summary>
    /// Check if any of the protected ranges overlap with the provided range
    /// </summary>
    /// <param name="range">The range to check</param>
    /// <exception cref="AccessViolationException">When the range overlaps protected memory</exception>
    public void CheckAccessViolation(DataRange range)
    {
        if (range.IsSubRange) // Ignore sub ranges
            return;

        var overlappingRange = ProtectedDataRanges.FirstOrDefault(x => !x.IsSubRange && x.End > range.Start && x.Start < range.End);

        if (overlappingRange != default)
            throw new AccessViolationException($"Data range {range} ({range.Description}), would overlap protected memory at: {overlappingRange} ({overlappingRange.Description})");
    }

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

        if (vTable.RefCount == 1)
        {
            VTables.Add(vTable.Location, vTable);
            SetProtectedMemory(vTable.VTableMemory);
        }
    }

    public void UnRegisterVTable(VTable vTable)
    {
        vTable.RefCount--;
        Debug.Assert(vTable.RefCount >= 0, "VTable ref count is below zero!");

        if (vTable.RefCount == 0)
        {
            VTables.Remove(vTable.Location);
            RemoveProtectedMemory(vTable.VTableMemory);
        }
    }

    public static bool GetIsSizeValid(ReadOnlySpan<byte> data) => data.Length >= 8;
}
