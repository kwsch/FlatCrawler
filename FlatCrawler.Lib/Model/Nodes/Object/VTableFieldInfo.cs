namespace FlatCrawler.Lib;

/// <summary>
/// Represents a field pointer within a <see cref="VTable"/>.
/// </summary>
/// <param name="Index">Field Index</param>
/// <param name="Offset">Relative offset to where the field's serialized value is stored.</param>
/// <param name="Size">
/// How many bytes are allocated to store the field's serialized value.
/// NOTE: this is derived on object construction based on the amount of data remaining, and isn't precise.
/// </param>
public sealed record VTableFieldInfo(int Index, int Offset, int Size) : IFieldOffset
{
    public bool HasValue => Offset != 0;
}
