namespace FlatCrawler.Lib;

public class FieldObservations
{
    public FieldSizeTracker Size { get; }
    public FieldTypeTracker Type { get; } = new();

    public FieldObservations(FieldSizeTracker size) => Size = size;

    public void Observe(FlatBufferNodeField entry, int index, byte[] data)
    {
        if (!entry.HasField(index))
            return;
        Type.Observe(entry, index, data, Size);
    }

    public string Summary()
    {
        return $"{{{Size.Summary()}}} {Type.Summary()}";
    }
}
