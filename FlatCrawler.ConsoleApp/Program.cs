using System;
using System.IO;

namespace FlatCrawler.ConsoleApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 0)
                Crawl(args[0]);
            else
                Console.WriteLine("Open the console application with a valid FlatBuffer binary file to begin.");

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static void Crawl(string path)
        {
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
}
