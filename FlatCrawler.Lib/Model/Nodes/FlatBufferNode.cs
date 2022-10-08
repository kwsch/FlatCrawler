using System;

namespace FlatCrawler.Lib
{
    public sealed record FlatBufferNodeType
    {
        public TypeCode Type { get; init; } = TypeCode.Empty;
        public bool IsArray { get; init; } = false;

        public FlatBufferNodeType(TypeCode type, bool isArray)
        {
            Type = type;
            IsArray = isArray;
        }
    }

    public abstract record FlatBufferNode
    {
        public readonly FlatBufferNode? Parent;
        public readonly int Offset;

        public virtual string Name { get; set; } = "???";
        public abstract string TypeName { get; set; }

        public string FullNodeName => $"{Name} {{{TypeName}}}";

        protected FlatBufferNode(int offset, FlatBufferNode? parent = null)
        {
            Offset = offset;
            Parent = parent;
        }

        public virtual int GetChildIndex(FlatBufferNode? child) => -1;
    }
}
