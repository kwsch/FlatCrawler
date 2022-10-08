using System;

namespace FlatCrawler.Lib;

public readonly record struct FlatBufferNodeType(TypeCode Type, bool IsArray)
{
    // Default structs are zeroed. Any non-zero value is a valid node type definition.
    public bool IsDefined => Type is not 0;
}
