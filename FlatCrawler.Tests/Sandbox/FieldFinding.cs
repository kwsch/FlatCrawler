using System.IO;
using FlatCrawler.Lib;
using Xunit;

namespace FlatCrawler.Tests;

public static class FieldFinding
{
    private const string folder = @"D:\roms\sword_1.3.0\Dump\areas";

    /// <summary>
    /// Finds a FlatBuffer file that has a non-default value for the requested field.
    /// </summary>
    [Fact]
    public static void FindRootNodeWithField()
    {
        if (!Directory.Exists(folder))
            return;

        const int fieldIndex = 29;
        static bool Criteria(FlatBufferRoot root, FlatBufferFile _) => root.HasField(fieldIndex);
        var result = SearchUtil.FindFlatFile(folder, Criteria);
        if (result != null)
        {
            // result found in present in a_d0101 !
        }
    }

    /// <summary>
    /// Finds a FlatBuffer file that has a non-default value for the requested field.
    /// </summary>
    [Fact]
    public static void FindChildObjectWithField()
    {
        if (!Directory.Exists(folder))
            return;

        const int parentField = 4;
        const int fieldIndex = 12;
        static bool Criteria(FlatBufferRoot root, FlatBufferFile file)
        {
            var child = root.ReadAsObject(file.Data, parentField);
            return child.HasField(fieldIndex);
        }

        var result = SearchUtil.FindFlatFile(folder, Criteria);
        if (result != null)
        {
        }
    }

    /// <summary>
    /// Finds a FlatBuffer file that has a non-default value for the requested field.
    /// </summary>
    [Fact]
    public static void FindArrayEntryWithObjectWithField()
    {
        if (!Directory.Exists(folder))
            return;

        const int parentField = 0;
        const int fieldIndex = 3;
        static bool Criteria(FlatBufferRoot root, FlatBufferFile file)
        {
            var data = file.Data;
            var child = root.ReadAsTable(data, parentField);
            foreach (var entry in child.Entries)
            {
                var meta = entry.ReadAsObject(data, 0);
                return meta.HasField(fieldIndex);
            }

            return false;
        }

        var result = SearchUtil.FindFlatFile(folder, Criteria);
        if (result != null)
        {
        }
    }
}
