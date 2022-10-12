using System.Collections.Generic;

namespace FlatCrawler.Lib;

/// <summary>
/// FlatBuffer array node that contains a (specified by serialized value) amount of child nodes.
/// </summary>
public interface IArrayNode
{
    /// <summary>
    /// Gets the entry at the requested index.
    /// </summary>
    /// <param name="entryIndex">Entry requested</param>
    FlatBufferNode GetEntry(int entryIndex);

    /// <summary>
    /// Gets all entries for the node.
    /// </summary>
    public IReadOnlyList<FlatBufferNode> Entries { get; }

    /// <summary>
    /// Gets the first entry index that has the highest field index with a defined value (not default).
    /// </summary>
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

    /// <summary>
    /// Gets a list of all entry indexes that have data for the requested field.
    /// </summary>
    /// <param name="fIndex">Field index</param>
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

    /// <summary>
    /// Finds the first entry that has data for the requested field.
    /// </summary>
    /// <param name="fIndex">Field index</param>
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
