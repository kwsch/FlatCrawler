using System;

namespace FlatCrawler.Sandbox
{
    internal static class Program
    {
        private static void Main()
        {
            DumpPokeMemory.Crawl(@"D:\poke_memory.prmb");
            DumpShopData.Crawl(@"D:\shop_data.bin");

            Console.WriteLine("Done.");
        }
    }
}
