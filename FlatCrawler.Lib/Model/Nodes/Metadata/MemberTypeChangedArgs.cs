namespace FlatCrawler.Lib;

public sealed record MemberTypeChangedArgs(int MemberIndex, FBFieldInfo FieldInfo, FBType OldType, FBType NewType);
