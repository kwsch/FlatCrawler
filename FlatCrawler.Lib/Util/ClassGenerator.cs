using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatCrawler.Lib;

public static class ClassGenerator
{
    private const string Header =
        $"using System;\n" +
        $"using System.ComponentModel;\n" +
        $"using FlatSharp.Attributes;\n" +
        $"\n" +
        $"namespace pkNX.Structures.FlatBuffers;\n\n";

    private const string ClassAttributes = "[FlatBufferTable, TypeConverter(typeof(ExpandableObjectConverter))]";
    private const string WriteFunc = $"    public byte[] Write() => FlatBufferConverter.SerializeFrom(this);";

    private static bool IsUndefined(string name)
    {
        return name == "???";
    }

    /// <summary>
    /// Manual implementation of setting TitleCase, replacing underscores with a capitalized word
    /// </summary>
    /// <param name="value">String to convert</param>
    /// <returns>TitleCase string</returns>
    private static string GetCapitalized(ReadOnlySpan<char> value)
    {
        Span<char> tmp = stackalloc char[value.Length];
        int ctr = 0;
        bool nextUpper = true; // force first capitalized
        foreach (char c in value)
        {
            if (c == '_')
            {
                // If current is _, replace next with upper char.
                nextUpper = true;
            }
            else if (nextUpper)
            {
                // If previous was space, replace current with upper char.
                tmp[ctr++] = char.ToUpper(c);
                nextUpper = false;
            }
            else
            {
                tmp[ctr++] = c;
            }
        }
        return new string(tmp[..ctr]);
    }
    
    private static string GetMemberDef(int fieldId, FBFieldInfo field)
    {
        FBType type = field.Type;
        if (IsUndefined(field.Name))
            field.Name = $"Field_{fieldId:00}";

        string typeStr = type.TypeName;

        var result = new StringBuilder(256);
        result.Append("    ");

        result.AppendJoin(' ',
            $"[FlatBufferItem({fieldId:00})]",
            "public",
            (field.IsArray ? $"{typeStr}[]" : typeStr),
            field.Name,
            "{ get; set; }");

        if(field.IsArray)
            result.Append($" = Array.Empty<{typeStr}>();");
        else if (type.Type == TypeCode.String)
            result.Append(" = string.Empty;");

        result.Append('\n');

        return result.ToString();
    }

    private static string GenerateClassMembers(FBClass fbClass, out HashSet<FBClass> subClasses)
    {
        string classMembers = string.Empty;
        var members = fbClass.Members;
        subClasses = new HashSet<FBClass>();

        for (int i = 0; i < members.Count; ++i)
        {
            var member = members[i];

            if (member.Type is FBClass subClass)
            {
                subClasses.Add(subClass);

                if (IsUndefined(member.Type.TypeName))
                    member.Type.TypeName = $"{fbClass.TypeName}_Type_F{i:00}";
            }

            classMembers += GetMemberDef(i, member);
        }

        return classMembers;
    }

    public static void GeneratePkNXClass(FlatBufferRoot root, string SourceFilePath)
    {
        var tableNode = root.AllFields.FirstOrDefault(n => n != null && n.FieldInfo.HasShape(TypeCode.Object, true));
        if (tableNode == null)
            throw new ArgumentNullException(nameof(root), "File not explored yet.");

        // Find the member that contains the table to define the class name
        string archiveTypeName = tableNode.FieldInfo.Type.TypeName;
        if (IsUndefined(archiveTypeName))
        {
            // If the name is not set, use the filename in capitalized form
            archiveTypeName = GetCapitalized(Path.GetFileNameWithoutExtension(SourceFilePath));
            tableNode.TypeName = archiveTypeName;
        }

        StringBuilder fileContents = new();
        fileContents.AppendJoin('\n', Header);

        HashSet<FBClass> visitedSubClasses = new() { root.ObjectClass };
        Queue<FBClass> toProcess = new();

        {
            string archiveClassMembers = GenerateClassMembers(root.ObjectClass, out HashSet<FBClass> archiveSubClasses);
            string archiveClass = $"{ClassAttributes}\n" +
                                  $"public class {archiveTypeName}Archive : IFlatBufferArchive<{archiveTypeName}>\n{{\n" +
                                  $"{WriteFunc}\n" +
                                  $"\n" +
                                  $"{archiveClassMembers}" +
                                  $"}}\n";

            fileContents.Append(archiveClass);

            archiveSubClasses.ExceptWith(visitedSubClasses);

            foreach (FBClass subClass in archiveSubClasses)
                toProcess.Enqueue(subClass);
        }
        
        while (toProcess.Count > 0)
        {
            FBClass subClass = toProcess.Dequeue();
            visitedSubClasses.Add(subClass);

            string classMembers = GenerateClassMembers(subClass, out HashSet<FBClass> subClasses);
            string objectClass = $"\n{ClassAttributes}\n" +
                                 $"public class {subClass.TypeName}\n" +
                                 $"{{\n" +
                                 $"{classMembers}" +
                                 $"}}\n";

            fileContents.Append(objectClass);

            subClasses.ExceptWith(visitedSubClasses);
            foreach (FBClass c in subClasses)
                toProcess.Enqueue(c);
        }

        string fileName = $"{archiveTypeName}Archive.cs";
        File.WriteAllText(fileName, fileContents.ToString());

        Console.WriteLine($"Flat buffer structure has been written to \"{Path.Join(Directory.GetCurrentDirectory(), fileName)}\".");
    }
}
