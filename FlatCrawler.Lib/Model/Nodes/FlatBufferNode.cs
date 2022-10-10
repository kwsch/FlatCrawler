namespace FlatCrawler.Lib;

public abstract record FlatBufferNode(int Offset, FlatBufferNode? Parent = null)
{
    public readonly FlatBufferNode? Parent = Parent;
    public FBFieldInfo FieldInfo = new();

    public readonly int Offset = Offset;
    public virtual string Name { get => FieldInfo.Name; set => FieldInfo.Name = value; }
    public virtual string TypeName { get => FieldInfo.Type.TypeName; set => FieldInfo.Type.TypeName = value; }

    public string FullNodeName => $"{Name} {{{TypeName}}}";

    public virtual int GetChildIndex(FlatBufferNode? child) => -1;
}
