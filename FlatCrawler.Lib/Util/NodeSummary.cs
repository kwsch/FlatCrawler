using System.Collections.Generic;

namespace FlatCrawler.Lib
{
    public static class NodeSummary
    {
        public static List<string> GetSummary(this FlatBufferNode node)
        {
            var result = new List<string> { $"{node.Name} @ 0x{node.Offset:X}" };
            switch (node)
            {
                case FlatBufferRoot root:
                    result.Add($"Magic: {root.Magic}");
                    result.Add($"DataTable Offset: 0x{root.DataTableOffset:X}");
                    result.Add(root.VTable.ToString());
                    break;

                case FlatBufferObject obj:
                    result.Add($"DataTable Offset: 0x{obj.DataTableOffset:X}");
                    result.Add(obj.VTable.ToString());
                    break;

                case FlatBufferStringValue utf:
                    result.Add($"UTF8 String: {utf.Value}");
                    break;

                case FlatBufferTableObject     table: result.Add($"Table Length: {table.Length}"); result.Add($"First Entry @ 0x{table.GetEntry(0).Offset:X}"); break;
                case FlatBufferTableString table: result.Add($"Table Length: {table.Length}"); result.Add($"First Entry @ 0x{table.GetEntry(0).Offset:X}"); break;

                case FlatBufferTableStruct<bool  > b: result.Add($"Table Length: {b.Length}"); result.Add($"First Entry @ 0x{b.GetEntry(0).Offset:X}"); break;
                case FlatBufferTableStruct<sbyte > b: result.Add($"Table Length: {b.Length}"); result.Add($"First Entry @ 0x{b.GetEntry(0).Offset:X}"); break;
                case FlatBufferTableStruct<short > b: result.Add($"Table Length: {b.Length}"); result.Add($"First Entry @ 0x{b.GetEntry(0).Offset:X}"); break;
                case FlatBufferTableStruct<int   > b: result.Add($"Table Length: {b.Length}"); result.Add($"First Entry @ 0x{b.GetEntry(0).Offset:X}"); break;
                case FlatBufferTableStruct<long  > b: result.Add($"Table Length: {b.Length}"); result.Add($"First Entry @ 0x{b.GetEntry(0).Offset:X}"); break;
                case FlatBufferTableStruct<byte  > b: result.Add($"Table Length: {b.Length}"); result.Add($"First Entry @ 0x{b.GetEntry(0).Offset:X}"); break;
                case FlatBufferTableStruct<ushort> b: result.Add($"Table Length: {b.Length}"); result.Add($"First Entry @ 0x{b.GetEntry(0).Offset:X}"); break;
                case FlatBufferTableStruct<uint  > b: result.Add($"Table Length: {b.Length}"); result.Add($"First Entry @ 0x{b.GetEntry(0).Offset:X}"); break;
                case FlatBufferTableStruct<ulong > b: result.Add($"Table Length: {b.Length}"); result.Add($"First Entry @ 0x{b.GetEntry(0).Offset:X}"); break;
                case FlatBufferTableStruct<float > b: result.Add($"Table Length: {b.Length}"); result.Add($"First Entry @ 0x{b.GetEntry(0).Offset:X}"); break;
                case FlatBufferTableStruct<double> b: result.Add($"Table Length: {b.Length}"); result.Add($"First Entry @ 0x{b.GetEntry(0).Offset:X}"); break;

                case FlatBufferFieldValue<bool  > b: result.Add($"Value: 0x{b.Value} [{b.Value}]"); break;
                case FlatBufferFieldValue<sbyte > b: result.Add($"Value: 0x{b.Value:X} [{b.Value}]"); break;
                case FlatBufferFieldValue<short > b: result.Add($"Value: 0x{b.Value:X} [{b.Value}]"); break;
                case FlatBufferFieldValue<int   > b: result.Add($"Value: 0x{b.Value:X} [{b.Value}]"); break;
                case FlatBufferFieldValue<long  > b: result.Add($"Value: 0x{b.Value:X} [{b.Value}]"); break;
                case FlatBufferFieldValue<byte  > b: result.Add($"Value: 0x{b.Value:X} [{b.Value}]"); break;
                case FlatBufferFieldValue<ushort> b: result.Add($"Value: 0x{b.Value:X} [{b.Value}]"); break;
                case FlatBufferFieldValue<uint  > b: result.Add($"Value: 0x{b.Value:X} [{b.Value}]"); break;
                case FlatBufferFieldValue<ulong > b: result.Add($"Value: 0x{b.Value:X} [{b.Value}]"); break;
                case FlatBufferFieldValue<float > b: result.Add($"Value: 0x{b.Value} [{b.Value}]"); break;
                case FlatBufferFieldValue<double> b: result.Add($"Value: 0x{b.Value} [{b.Value}]"); break;

                default:
                    result.Add($"Not implemented: {node}");
                    break;
            }

            return result;
        }
    }
}
