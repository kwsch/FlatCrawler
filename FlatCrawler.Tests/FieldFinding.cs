using System.IO;
using FlatCrawler.Lib;
using Xunit;

namespace FlatCrawler.Sandbox;

public static class FieldFinding
{
    /// <summary>
    /// Finds a FlatBuffer file that has a non-default value for the requested field.
    /// </summary>
    [Fact]
    public static void FindRootNodeWithField()
    {
        const string folder = @"D:\roms\sword_1.3.0\Dump\areas";
        if (!Directory.Exists(folder))
            return;

        const int fieldIndex = 29;
        static bool Criteria(FlatBufferRoot root, byte[] _) => root.HasField(fieldIndex);
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
        const string folder = @"D:\roms\sword_1.3.0\Dump\areas";
        if (!Directory.Exists(folder))
            return;

        const int parentField = 4;
        const int fieldIndex = 12;
        static bool Criteria(FlatBufferRoot root, byte[] data)
        {
            var child = root.ReadObject(parentField, data);
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
        const string folder = @"D:\roms\sword_1.3.0\Dump\areas";
        if (!Directory.Exists(folder))
            return;

        const int parentField = 0;
        const int fieldIndex = 3;
        static bool Criteria(FlatBufferRoot root, byte[] data)
        {
            var child = root.ReadArrayObject(parentField, data);
            foreach (var entry in child.Entries)
            {
                var meta = entry.ReadObject(0, data);
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
