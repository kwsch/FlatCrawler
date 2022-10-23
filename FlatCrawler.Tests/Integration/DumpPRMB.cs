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
        SwordShieldPRMB.Export(Resources.poke_memory, "memory", 0x1F);
    }
}
