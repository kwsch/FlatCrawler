using System.Collections.Generic;

namespace FlatCrawler.Lib;

public interface IArrayNode
{
    FlatBufferNode GetEntry(int entryIndex);
    public IReadOnlyList<FlatBufferNode> Entries { get; }

    (int Index, int Max) GetMaxFieldCountIndex()
    {
        int index = 0;
        int max = 0;
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i] is not FlatBufferNodeField f || f.FieldCount <= max)
                continue;
            index = i;
            max = f.FieldCount;
        }
        return (index, max);
    }

    List<int> GetEntryIndexesWithField(int fIndex)
    {
        var result = new List<int>();
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i] is not FlatBufferNodeField f || !f.HasField(fIndex))
                continue;
            result.Add(i);
        }
        return result;
    }

    int GetEntryIndexWithField(int fIndex)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i] is not FlatBufferNodeField f || !f.HasField(fIndex))
                continue;
            return i;
        }
        return -1;
    }
}
