namespace FlatCrawler.Lib
{
    public abstract record FlatBufferNode
    {
        public readonly FlatBufferNode? Parent;
        public readonly int Offset;

        public abstract string Name { get; }

        protected FlatBufferNode(int offset, FlatBufferNode? parent = null)
        {
            Offset = offset;
            Parent = parent;
        }

        public virtual int GetChildIndex(FlatBufferNode? child) => -1;
    }
}
