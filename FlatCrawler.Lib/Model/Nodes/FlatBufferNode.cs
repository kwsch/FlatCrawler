namespace FlatCrawler.Lib;

public abstract record FlatBufferNode(int Offset, FlatBufferNode? Parent = null)
{
    public readonly FlatBufferNode? Parent = Parent;
    public FBFieldInfo FieldInfo = new();

    public readonly int Offset = Offset;
    public virtual string Name { get; set; } = "???";
    public abstract string TypeName { get; set; }

    public string FullNodeName => $"{Name} {{{TypeName}}}";

    public virtual int GetChildIndex(FlatBufferNode? child) => -1;
}
