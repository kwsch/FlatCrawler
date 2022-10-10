using System;

namespace FlatCrawler.Lib
{
    public record FBType
    {
        public TypeCode Type { get; } = TypeCode.Empty;
        public string TypeName { get; set; } = string.Empty;

        public FBType(TypeCode type = TypeCode.Empty)
        {
            Type = type;
            TypeName = type.ToString().ToLowerInvariant();
        }

        // Default structs are zeroed. Any non-zero value is a valid node type definition.
        public bool IsDefined => Type != TypeCode.Empty;
    }
}
