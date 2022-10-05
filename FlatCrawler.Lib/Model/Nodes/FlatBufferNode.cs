using System.Xml.Linq;

namespace FlatCrawler.Lib
{
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
