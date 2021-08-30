using System;

namespace FlatCrawler.Lib
{
    public static class CommandUtil
    {
        public static TypeCode GetTypeCode(string type) => type.Replace(" ", "").Replace("[]", "") switch
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
    }
}
