namespace FlatCrawler.Lib;

public sealed record FlatBufferUnionNode : FlatBufferNode
{
    public FlatBufferNodeType Type { get; }
    public FlatBufferNode Inner { get; }

    public FlatBufferUnionNode(FlatBufferNodeType Type, FlatBufferObject Parent, FlatBufferNode Inner) : base(Parent)
    {
        this.Type = Type;
        this.Inner = Inner;
    }

    public override string TypeName { get => $"{Type} {Inner.TypeName}"; set { } }
}
