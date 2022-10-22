using System;
using System.IO;
using FlatCrawler.Lib;

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
            Console.WriteLine("Type \"open\" (no-quotes) and paste the path of the FlatBuffer you would like to analyze.");
            Console.Write(">>> ");
            var cmd = Console.ReadLine();
            if (cmd is not { } x)
                continue;
            x = x.Trim();
            if (x.StartsWith("quit", StringComparison.InvariantCultureIgnoreCase))
                break;

            const string rip = "rip";
            if (x.StartsWith(rip, StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Ripping...");
                var src = GetFileNameFromCommandLineInput(x[(rip.Length + 1)..].Trim());
                FileAnalysis.IterateAndDump(src);
                Console.WriteLine("Done.");
                continue;
            }

            const string analyze = "analyze";
            if (x.StartsWith(analyze, StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Analyzing...");
                var src = GetFileNameFromCommandLineInput(x[(analyze.Length + 1)..].Trim());
                var dst = FileAnalysis.GetExecutableAnalysisDumpFolder();
                var settings = new FileAnalysisSettings(src, dst) { DumpIndividualSchemaAnalysis = false };
                FileAnalysis.IterateAndDumpSame(Console.Out, settings);
                Console.WriteLine("Done.");
                continue;
            }

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

    private static string GetFileNameFromCommandLineInput(string path) => Path.TrimEndingDirectorySeparator(path.Replace("\"", ""));

    private static void Crawl(string path)
    {
        path = GetFileNameFromCommandLineInput(path);
        if (!File.Exists(path))
        {
            Console.WriteLine("File does not exist.");
            return;
        }
        var file = new FlatBufferFile(path);
        if (!file.IsValid)
        {
            Console.WriteLine("Not a valid flat buffer file.");
            return;
        }

        var crawler = new ConsoleCrawler(path, file);
        crawler.CrawlLoop();
    }
}
