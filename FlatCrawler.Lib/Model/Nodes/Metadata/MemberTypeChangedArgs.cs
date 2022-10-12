using System;

namespace FlatCrawler.Lib;

public sealed record MemberTypeChangedArgs(int MemberIndex, byte[] Data, FBFieldInfo FieldInfo, FBType OldType, FBType NewType);
