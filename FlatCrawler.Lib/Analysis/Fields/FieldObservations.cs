using System;

namespace FlatCrawler.Lib;

public class FieldObservations
{
    public FieldSizeTracker Size { get; }
    public FieldTypeTracker Type { get; } = new();

    public FieldObservations(FieldSizeTracker size) => Size = size;

    public void Observe(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data)
    {
        if (!entry.HasField(index))
            return;
        Type.Observe(entry, index, data, Size);
    }

    public string Summary() => $"{{{Size.Summary()}}} {Type.Summary()}";
    public string Summary(FlatBufferNodeField node, int index, ReadOnlySpan<byte> data) => $"{{{Size.Summary()}}} {Type.Summary(node, index, data)}";
}
