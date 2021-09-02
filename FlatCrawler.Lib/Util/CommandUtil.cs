using System;
using System.Globalization;

namespace FlatCrawler.Lib
{
    public static class CommandUtil
    {
        private static TypeCode GetTypeCode(string type) => type.Replace(" ", "").Replace("[]", "") switch
        {
            "bool" => TypeCode.Boolean,

            "sbyte" or "s8" => TypeCode.SByte,
            "short" or "s16" => TypeCode.Int16,
            "int" or "s32" => TypeCode.Int32,
            "long" or "s64" => TypeCode.Int64,

            "byte" or "u8" or "i8" => TypeCode.Byte,
            "ushort" or "u16" or "i16" => TypeCode.UInt16,
            "uint" or "u32" or "i32" => TypeCode.UInt32,
            "ulong" or "u64" or "i64" => TypeCode.UInt64,

            "float" => TypeCode.Single,
            "double" => TypeCode.Double,
            _ => TypeCode.Empty,
        };

        public static (int FieldIndex, string FieldType) GetDualArgs(string args)
        {
            var argSplit = args.Split(' ');
            return (int.Parse(argSplit[0]), argSplit[1]);
        }

        public static int GetIntPossibleHex(string txt) => txt.Contains("0x")
            ? int.Parse(txt.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : int.Parse(txt, CultureInfo.InvariantCulture);

        public static FlatBufferNode ReadNode(this FlatBufferNode node, int fieldIndex, string type, byte[] data) => node switch
        {
            IArrayNode a => a.GetEntry(fieldIndex),
            FlatBufferNodeField r => ReadNode(r, fieldIndex, data, type),
            _ => throw new ArgumentException("Field not present in VTable"),
        };

        private static FlatBufferNode ReadNode(FlatBufferNodeField node, int fieldIndex, byte[] data, string type)
        {
            FlatBufferNode result = type switch
            {
                "string" or "str" => node.ReadString(fieldIndex, data),
                "object" => node.ReadObject(fieldIndex, data),

                "table" or "object[]" => node.ReadArrayObject(fieldIndex, data),
                "string[]" => node.ReadArrayString(fieldIndex, data),

                _ => GetStructureNode(node, fieldIndex, data, type),
            };
            node.SetFieldHint(fieldIndex, type);
            node.TrackChildFieldNode(fieldIndex, result);
            return result;
        }

        private static FlatBufferNode GetStructureNode(FlatBufferNodeField node, int fieldIndex, byte[] data, string type)
        {
            var typecode = GetTypeCode(type);
            if (type.Contains("[]")) // table-array
                return node.GetTableStruct(fieldIndex, data, typecode);
            return node.GetFieldValue(fieldIndex, data, typecode);
        }
    }
}
