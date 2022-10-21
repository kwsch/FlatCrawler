using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlatCrawler.Lib;

public static class SearchUtil
{
    public static string? FindFlatFile(string folder, Func<FlatBufferRoot, FlatBufferFile, bool> criteria)
    {
        return FindFlatFiles(folder, criteria).FirstOrDefault();
    }

    public static IEnumerable<string> FindFlatFiles(string folder, Func<FlatBufferRoot, FlatBufferFile, bool> criteria)
    {
        var files = Directory.EnumerateFiles(folder);
        return FindFlatFiles(files, criteria);
    }

    public static IEnumerable<string> FindFlatFiles(IEnumerable<string> files, Func<FlatBufferRoot, FlatBufferFile, bool> criteria)
    {
        foreach (string filePath in files)
        {
            try
            {
                var file = new FlatBufferFile(filePath);
                var root = FlatBufferRoot.Read(file, 0);
                if (!criteria(root, file))
                    continue;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Unable to parse file: {Path.GetFileNameWithoutExtension(filePath)}", ex);
            }
            yield return filePath;
        }
    }
}
