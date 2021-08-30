using System.Collections.Generic;

namespace FlatCrawler.Lib
{
    public interface IArrayNode
    {
        FlatBufferNode GetEntry(int entryIndex);
        public IReadOnlyList<FlatBufferNode> Entries { get; }
    }
}
