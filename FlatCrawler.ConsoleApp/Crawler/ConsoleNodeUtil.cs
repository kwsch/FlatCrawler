using System;
using FlatCrawler.Lib;

namespace FlatCrawler.ConsoleApp;

public static class ConsoleNodeUtil
{
    public static void Print(this FlatBufferNode node)
    {
        foreach (var line in node.GetSummary())
            Console.WriteLine(line);
    }

    public static void PrintTree(this FlatBufferNode node)
    {
        var printer = new FlatBufferPrinter();
        printer.GeneratePrint(node, Console.Out);
    }
}
