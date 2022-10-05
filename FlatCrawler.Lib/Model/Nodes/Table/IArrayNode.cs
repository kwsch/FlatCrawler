using System;
using System.Collections.Generic;

namespace FlatCrawler.Lib
{
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

        Dictionary<byte, Union> GetUnionAnalysis(byte[] data)
        {
            var result = new Dictionary<byte, Union>();
            var entries = Entries;
            for (var index = 0; index < entries.Count; index++)
            {
                var flatBufferNode = entries[index];
                var node = (FlatBufferObject)flatBufferNode;
                var type = node.GetFieldValue(0, data, TypeCode.Byte);
                var obj = node.ReadObject(1, data);
                var bval = ((FlatBufferFieldValue<byte>)type).Value;
                var chk = new Union(bval, index, obj.FieldCount);
                node.TypeName = chk.ToString();

                // add or update key if our FieldCount is new or bigger than previously noted for this union type
                if (!result.TryGetValue(bval, out var c) || c.FieldCount < chk.FieldCount)
                    result[bval] = chk;
            }
            return result;
        }

        public sealed record Union(byte Type, int Index, int FieldCount);
    }
}
