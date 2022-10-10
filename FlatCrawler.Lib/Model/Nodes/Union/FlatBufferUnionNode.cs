namespace FlatCrawler.Lib;

public sealed record FlatBufferUnionNode : FlatBufferNode
{
    public FlatBufferNode Inner { get; }

    public FlatBufferUnionNode(FBFieldInfo info, FlatBufferObject Parent, FlatBufferNode Inner) : base(Parent)
    {
        FieldInfo = info;
        this.Inner = Inner;
    }

    public override string TypeName { get => $"{FieldInfo.Type} {Inner.TypeName}"; set { } }
}
