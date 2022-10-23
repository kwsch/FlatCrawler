using System;
using System.Collections.Generic;
using System.Linq;

namespace FlatCrawler.Lib;

public sealed record UnionAnalysisResult(Dictionary<byte, UnionNodeSummary> Groups)
{
    public byte[] UniqueTypeCodes => Groups.Keys.OrderBy(z => z).ToArray();
    public IEnumerable<int> GetIndexes(byte typeCode) => Groups[typeCode].Entries.Select(z => z.Index);
    public int MaxFieldCount(byte typeCode) => Groups[typeCode].Entries.Max(z => z.Node.FieldCount);
    public bool SameFieldCount(byte typeCode) => Groups[typeCode].Entries.Select(z => z.Node.FieldCount).Distinct().Count() == 1;

    public FieldAnalysisResult AnalyzeNodesWithType(byte type, ReadOnlySpan<byte> data)
    {
        var summaries = Groups[type];
        var entries = summaries.Entries
            .Select(z => z.Node)
            .Cast<FlatBufferNodeField>()
            .ToArray();

        return FieldAnalysis.AnalyzeFields(data, entries);
    }
}
