using System;

namespace FlatCrawler.Lib;

/// <summary>
/// Tracks metadata about the shape of a field across many node fields.
/// </summary>
public sealed class FieldObservations(FieldSizeTracker size)
{
    /// <summary> The number of bytes that the field occupies. </summary>
    public FieldSizeTracker Size { get; } = size;

    /// <summary> Potential type(s) of the field. </summary>
    public FieldTypeTracker Type { get; } = new();

    /// <summary>
    /// Updates the <see cref="Type"/> observations with the specified node.
    /// </summary>
    /// <param name="entry">Node to check</param>
    /// <param name="index">Field index of node</param>
    /// <param name="data">Data to interpret field with</param>
    public void Observe(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data)
    {
        if (!entry.HasField(index))
            return;
        Type.Observe(entry, index, data, Size);
    }

    /// <summary> Gets a simple summary of this metadata. </summary>
    public string Summary() => $"{{{Size.Summary()}}} {Type.Summary()}";

    /// <summary>
    /// Gets a more detailed summary of this metadata by interpreting the field with the specified data.
    /// </summary>
    public string Summary(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data) => $"{{{Size.Summary()}}} {Type.Summary(entry, index, data)}";

    public override int GetHashCode() => Type.GetHashCode();
}
