namespace FlatCrawler.Lib;

public enum FieldOrder
{
    Unchecked,
    DecreasingSize, // Best packing size
    IncreasingSize, // Average packing size
    Mixed, // Worst packing size
}
