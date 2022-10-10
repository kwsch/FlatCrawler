using System;

namespace FlatCrawler.Lib
{
    public record FBType
    {
        public TypeCode Type { get; } = TypeCode.Empty;
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// The size of the type in bytes. For classes this would be the size of all fields combined.
        /// </summary>
        public int Size { get; protected set; } = 0;

        public FBType(TypeCode type = TypeCode.Empty)
        {
            Type = type;
            TypeName = type.ToString().ToLowerInvariant();
        }

        // Default structs are zeroed. Any non-zero value is a valid node type definition.
        public bool IsDefined => Type != TypeCode.Empty;
    }
}
