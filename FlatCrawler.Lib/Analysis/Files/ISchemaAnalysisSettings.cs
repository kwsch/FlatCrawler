namespace FlatCrawler.Lib;

public interface ISchemaAnalysisSettings
{
    int MaxRecursionDepth { get; }
}

public sealed class SchemaAnalysisSettings : ISchemaAnalysisSettings
{
    public int MaxRecursionDepth { get; set; } = 5;
}
