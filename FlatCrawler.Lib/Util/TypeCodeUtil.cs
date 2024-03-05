using System;

namespace FlatCrawler.Lib;

/// <summary>
/// Parses a type code string into a <see cref="TypeCode"/> and a boolean indicating whether the type is an array.
/// </summary>
public static class TypeCodeUtil
{
    /// <summary> Unrecognized type code -- treat as invalid. </summary>
    public const TypeCode Unrecognized = TypeCode.Empty;

    public static (bool AsArray, TypeCode Type) GetTypeCodeTuple(ReadOnlySpan<char> text)
    {
        text = text.Trim();
        if (text.Length > 20)
            return (false, Unrecognized);

        bool asArray = false;
        if (text.EndsWith("[]", StringComparison.Ordinal))
        {
            asArray = true;
            text = text[..^2];
        }

        if (text.Equals("table", StringComparison.OrdinalIgnoreCase))
            return (true, TypeCode.Object);

        Span<char> tmp = stackalloc char[text.Length];
        text.ToLowerInvariant(tmp);
        var type = GetTypeCode(tmp);
        return (asArray, type);
    }

    /// <summary>
    /// Parses a type code string into a <see cref="TypeCode"/>.
    /// </summary>
    public static TypeCode GetTypeCode(ReadOnlySpan<char> type) => type switch
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

        "float" or "single" => TypeCode.Single,
        "double" => TypeCode.Double,

        "string" or "str" => TypeCode.String,
        "object" or "obj" or "table" or "union" => TypeCode.Object,
        _ => Unrecognized,
    };

    /// <summary>
    /// Checks if the type code is a recognized node type.
    /// </summary>
    public static bool IsValidNodeType(this TypeCode type) => type is not (<= Unrecognized or TypeCode.Decimal or TypeCode.DateTime or TypeCode.Char or > TypeCode.String);

    public static string ToTypeString(this TypeCode type) => type switch
    {
        TypeCode.Int16 => "short",
        TypeCode.Int32 => "int",
        TypeCode.Int64 => "long",
        TypeCode.UInt16 => "ushort",
        TypeCode.UInt32 => "uint",
        TypeCode.UInt64 => "ulong",
        TypeCode.Single => "float",
        TypeCode.Boolean => "bool",
        _ => type.ToString().ToLowerInvariant(),
    };
}
