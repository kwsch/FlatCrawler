using System;
using System.Collections.Generic;
using System.IO;

namespace FlatCrawler.Lib;

/// <summary>
/// Draft file object
/// </summary>
public sealed class FlatBufferFile
{
    // Data should never overlap with any of the data ranges in this set
    public readonly SortedSet<DataRange> ProtectedDataRanges = new();

    private readonly SortedDictionary<int, VTable> VTables = new();

    private byte[] _data { get; } = Array.Empty<byte>();
    public ReadOnlySpan<byte> Data => _data;

    public bool IsValid => GetIsSizeValid(Data);

    public FlatBufferFile(string path) : this(File.ReadAllBytes(path)) { }
    public FlatBufferFile(byte[] data) => _data = data;

    /// <summary>
    /// Read the VTable at <paramref name="offset"/>.
    /// If a VTable was already registered at that location, the registered VTable will be returned.
    /// </summary>
    /// <param name="offset">The offset of the VTable from the start of the file in bytes.</param>
    /// <returns>The VTable at <paramref name="offset"/></returns>
    public VTable GetOrReadVTable(int offset)
    {
        if (VTables.TryGetValue(offset, out VTable? table))
            return table;

        VTable vtable = new(Data, offset);
        VTables.Add(vtable.Location, vtable);
        return vtable;
    }

    public static bool GetIsSizeValid(ReadOnlySpan<byte> data) => data.Length >= 8;
}
