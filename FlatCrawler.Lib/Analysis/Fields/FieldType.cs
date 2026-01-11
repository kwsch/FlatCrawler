using System;

namespace FlatCrawler.Lib;

[Flags]
public enum FieldType
{
    Unknown = 0,

    StructSingle  = 1 << 0,
    StructArray   = 1 << 1,
    StructInlined = 1 << 2,

    StructValue = StructSingle | StructInlined,
    StructType  = StructSingle | StructArray | StructInlined,

    Object        = 1 << 3,
    ObjectArray   = 1 << 4,
    ObjectUnion   = 1 << 5,

    ReferenceType = Object | ObjectArray | ObjectUnion,

    All = StructType | ReferenceType,
}
