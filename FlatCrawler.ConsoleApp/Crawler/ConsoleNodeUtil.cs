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
        var result = printer.GeneratePrint(node);
        foreach (var line in result)
            Console.WriteLine(line);
    }
}
