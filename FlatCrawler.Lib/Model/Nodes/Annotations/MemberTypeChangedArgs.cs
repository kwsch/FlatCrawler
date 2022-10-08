namespace FlatCrawler.Lib;

public sealed record MemberTypeChangedArgs(int MemberIndex, FlatBufferNodeType? OldType, FlatBufferNodeType NewType);
