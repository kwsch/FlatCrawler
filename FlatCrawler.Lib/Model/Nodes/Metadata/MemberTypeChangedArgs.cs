using System;

namespace FlatCrawler.Lib;

public readonly ref struct MemberTypeChangedArgs
{
    public int MemberIndex { get; }
    public ReadOnlySpan<byte> Data { get; }
    public FBFieldInfo FieldInfo { get; }
    public FBType OldType { get; }
    public FBType NewType { get; }

    public MemberTypeChangedArgs(int memberIndex, ReadOnlySpan<byte> data, FBFieldInfo fieldInfo, FBType oldType, FBType newType)
    {
        MemberIndex = memberIndex;
        Data = data;
        FieldInfo = fieldInfo;
        OldType = oldType;
        NewType = newType;
    }
}
