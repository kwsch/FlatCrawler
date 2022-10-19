namespace FlatCrawler.Lib;

public sealed record FileAnalysisResult(int FieldCount, int Hash, string FileName, string Path);
