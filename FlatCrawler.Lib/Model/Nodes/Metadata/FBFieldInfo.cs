using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatCrawler.Lib;

public record FBFieldInfo
{
    public string Name { get; set; } = "???";
    public FBType Type { get; init; } = new();
    public bool IsArray { get; init; } = false;
    public int Size { get; init; } = 0;
    public int OffsetInVTable { get; init; } = 0;

    public override string ToString()
    {
        return $"{Name} {{ Type: {Type.TypeName}{(IsArray ? "[]" : "")}, Size: {Size}, Offset: {OffsetInVTable} }}";
    }
}

