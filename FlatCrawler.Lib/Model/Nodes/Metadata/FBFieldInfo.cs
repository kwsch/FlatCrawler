namespace FlatCrawler.Lib;

public record FBFieldInfo
{
    public string Name { get; set; } = "???";
    public FBType Type { get; init; } = new();
    public bool IsArray { get; init; } = false;

    /// <summary>
    /// The offset in the data table. Field value is located at `DataTableLocation` + Offset.
    /// </summary>
    public int Offset { get; init; } = 0;

    /// <summary>
    /// The size of the field value in bytes 
    /// </summary>
    public int Size { get; init; } = 0;

    public override string ToString()
    {
        return $"{Name} {{ Type: {Type.TypeName}{(IsArray ? "[]" : "")}, Size: {Size}, Offset: {Offset} }}";
    }
}

