using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlatCrawler.Lib
{
    public static class SearchUtil
    {
        public static string? FindFlatFile(string folder, Func<FlatBufferRoot, byte[], bool> criteria)
        {
            return FindFlatFiles(folder, criteria).FirstOrDefault();
        }

        public static IEnumerable<string> FindFlatFiles(string folder, Func<FlatBufferRoot, byte[], bool> criteria)
        {
            var files = Directory.EnumerateFiles(folder);
            return FindFlatFiles(files, criteria);
        }

        public static IEnumerable<string> FindFlatFiles(IEnumerable<string> files, Func<FlatBufferRoot, byte[], bool> criteria)
        {
            foreach (string file in files)
            {
                var data = File.ReadAllBytes(file);
                var root = FlatBufferRoot.Read(0, data);
                if (criteria(root, data))
                    yield return file;
            }
        }
    }
}
