using System.IO;
using FlatCrawler.Tests.Properties;
using Xunit;

namespace FlatCrawler.Tests;

/// <summary>
/// Uses test resources to parse and export data serialized to PRMB-schema FlatBuffer files.
/// </summary>
public static class DumpPRMB
{
    [Fact]
    public static void Crawl()
    {
        SwordShieldPRMB.Export(Resources.pokecamp_foodstuff_table, "foodstuff", 2);
        SwordShieldPRMB.Export(Resources.pokecamp_kinomi_table, "kinomi", 7);
        SwordShieldPRMB.Export(Resources.poke_memory, "memory", 31);
    }

    private const string SWSH = @"D:\roms\sword_1.3.0\rom";

    [Theory]
    [InlineData(@"bin\appli\townmap\bin\map_destination_data.prmb", 5)]
    [InlineData(@"bin\appli\townmap\bin\map_data.prmb", 14)]
    [InlineData(@"bin\appli\pw\data_table\pw.prmb", 48)] // pokejobs
    public static void CrawlFile(string path, int width)
    {
        var file = Path.Combine(SWSH, path);
        if (!File.Exists(file))
            return; // skip
        var data = File.ReadAllBytes(file);
        SwordShieldPRMB.Export(data, Path.GetFileNameWithoutExtension(path), width);
    }
}
