using System;
using System.IO;

namespace FlatCrawler.ConsoleApp;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length != 0)
            Crawl(args[0]);
        StartCrawl();
    }

    private static void StartCrawl()
    {
        while (true)
        {
            Console.WriteLine("Type \"open\" (no-quotes) and paste the path of the FlatBuffer you would like to analyze");
            Console.Write(">>> ");
            var cmd = Console.ReadLine();
            if (cmd is not { } x)
                continue;
            x = x.Trim();
            if (x.StartsWith("quit", StringComparison.InvariantCultureIgnoreCase))
                break;

            if (!x.StartsWith("open", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Try again.");
                continue;
            }

            var space = x.IndexOf(' ');
            var path = x[(space + 2)..];
            Crawl(path);
        }
    }

    private static void Crawl(string path)
    {
        path = Path.TrimEndingDirectorySeparator(path.Replace("\"", ""));
        if (!File.Exists(path))
        {
            Console.WriteLine("File does not exist.");
            return;
        }
        var data = File.ReadAllBytes(path);
        var crawler = new ConsoleCrawler(path, data);
        crawler.CrawlLoop();
    }
}
