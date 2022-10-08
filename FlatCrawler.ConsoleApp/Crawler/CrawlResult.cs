using static FlatCrawler.ConsoleApp.CrawlResult;

namespace FlatCrawler.ConsoleApp;

public enum CrawlResult
{
    Navigate,
    Update,
    Silent,
    Quit,
    Unrecognized,
    Error,
}

public static class CrawlResultExtensions
{
    public static bool IsSavedNavigation(this CrawlResult c) => c is Navigate or Update;
}
