using System;
using FlatCrawler.Lib;

namespace FlatCrawler.ConsoleApp;

public static class ConsoleNodeUtil
{
    extension(FlatBufferNode node)
    {
        public void Print()
        {
            foreach (var line in node.GetSummary())
                Console.WriteLine(line);
        }

        public void PrintTree()
        {
            var printer = new FlatBufferPrinter();
            printer.GeneratePrint(node, Console.Out);
        }
    }
}
