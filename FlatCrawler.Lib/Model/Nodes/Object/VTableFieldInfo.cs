namespace FlatCrawler.Lib;

public sealed class VTableFieldInfo
{
    public readonly int Index;
    public readonly int Offset;
    public readonly int Size;

    public VTableFieldInfo(int index, int offset, int size)
    {
        Index = index;
        Offset = offset;
        Size = size;
    }

    public override string ToString() => $"[{Offset:X4}] (Length: {Size})";
}
