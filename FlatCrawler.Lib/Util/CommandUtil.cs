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

            "string" or "str" => TypeCode.String,
            "object" or "obj" or "table" => TypeCode.Object,
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
            FlatBufferNodeField r => r.ReadNode(fieldIndex, data, type),
            _ => throw new ArgumentException("Field not present in VTable"),
        };

        private static FlatBufferNode ReadNode(this FlatBufferNodeField node, int fieldIndex, byte[] data, string type)
        {
            var code = GetTypeCode(type);
            bool asArray = type == "table" || type.Contains("[]");
            FlatBufferNode result = node.ReadNode(fieldIndex, data, code, asArray);

            node.SetFieldHint(fieldIndex, type);
            node.TrackChildFieldNode(fieldIndex, result);
            return result;
        }
    }
}
