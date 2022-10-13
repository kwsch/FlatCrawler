using System;

namespace FlatCrawler.Lib;

public record FBFieldInfo : IFieldOffset
{
    public string Name { get; set; } = "???";
    public FBType Type { get; init; } = new();
    public bool IsArray { get; init; }

    /// <summary>
    /// The offset in the data table. Field value is located at <see cref="FlatBufferNodeField.DataTableOffset"/> + Offset.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// The size of the field value in bytes
    /// </summary>
    public int Size { get; init; }

    public bool HasValue => Offset != 0;

    public override string ToString()
    {
        return $"{Name} {{ Type: {Type.TypeName}{(IsArray ? "[]" : "")}, Size: {Size}, Offset: {Offset} }}";
    }

    public bool HasShape(TypeCode type, bool asArray) => Type.Type == type && IsArray == asArray;
}
