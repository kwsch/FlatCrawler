using System;
using System.Collections.Generic;

namespace FlatCrawler.Lib
{
    public abstract record FlatBufferTable<T> : FlatBufferNode, IArrayNode where T : FlatBufferNode
    {
        public int Length { get; }
        public int DataTableOffset { get; }
        public T[] Entries { get; }

        IReadOnlyList<FlatBufferNode> IArrayNode.Entries => Entries;

        protected FlatBufferTable(int offset, FlatBufferNode parent, int length, int dataTableOffset) :
            base(offset, parent)
        {
            Length = length;
            DataTableOffset = dataTableOffset;
            Entries = new T[length];
        }

        public abstract FlatBufferNode GetEntry(int entryIndex);

        public override int GetChildIndex(FlatBufferNode? child)
        {
            if (child is null)
                return -1;
            return Array.FindIndex(Entries, z => ReferenceEquals(z, child));
        }
    }
}
