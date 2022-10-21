using System;

namespace FlatCrawler.Lib;

public sealed record FBFieldInfo
{
    public string Name { get; set; } = "???";
    public FBType Type { get; init; } = new();
    public bool IsArray { get; init; }

    /// <summary>
    /// The size of the field type in bytes
    /// </summary>
    public int Size { get; init; }

    public override string ToString()
    {
        return $"{Name} {{ Type: {Type.TypeName}{(IsArray ? "[]" : "")}, Size: {Size} }}";
    }

    public bool HasShape(TypeCode type, bool asArray) => Type.Type == type && IsArray == asArray;
}
