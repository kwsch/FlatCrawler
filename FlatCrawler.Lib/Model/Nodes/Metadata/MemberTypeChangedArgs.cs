using System;

namespace FlatCrawler.Lib;

public readonly ref struct MemberTypeChangedArgs(int memberIndex, ReadOnlySpan<byte> data, FBFieldInfo fieldInfo, FBType oldType, FBType newType)
{
    public int MemberIndex { get; } = memberIndex;
    public ReadOnlySpan<byte> Data { get; } = data;
    public FBFieldInfo FieldInfo { get; } = fieldInfo;
    public FBType OldType { get; } = oldType;
    public FBType NewType { get; } = newType;
}
