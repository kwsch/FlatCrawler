using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FlatCrawler.Lib;

/// <summary>
/// Draft file object
/// </summary>
public sealed class FlatBufferFile
{
    [Obsolete("Quick and dirty static instance access to make it easier to move to the new API")]
    public static FlatBufferFile Instance { get; private set; } = null!;

    // Data should never overlap with any of the data ranges in this set
    public readonly SortedSet<DataRange> ProtectedDataRanges = new();

    private readonly SortedDictionary<int, VTable> VTables = new();

    private ReadOnlyMemory<byte> _data;
    public ReadOnlySpan<byte> Data => _data.Span;

    public bool IsValid => GetIsSizeValid(Data);

    public FlatBufferFile(string path) : this(File.ReadAllBytes(path)) { }
    public FlatBufferFile(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray();
        Instance = this;
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
        }
    }

    public void UnRegisterVTable(VTable vTable)
    {
        vTable.RefCount--;
        Debug.Assert(vTable.RefCount >= 0, "VTable ref count is below zero!");

        if (vTable.RefCount == 0)
        {
            VTables.Remove(vTable.Location);
        }
    }

    public static bool GetIsSizeValid(ReadOnlySpan<byte> data) => data.Length >= 8;
}
