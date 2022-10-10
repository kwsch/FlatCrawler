namespace FlatCrawler.Lib;

public abstract record FlatBufferNode(int Offset, FlatBufferNode? Parent = null)
{
    public readonly FlatBufferNode? Parent = Parent;
    public FBFieldInfo FieldInfo { get; protected set; } = new();

    public readonly int Offset = Offset;
    public virtual string Name { get => FieldInfo.Name; set => FieldInfo.Name = value; }
    public virtual string TypeName { get => FieldInfo.Type.TypeName; set => FieldInfo.Type.TypeName = value; }

    public string FullNodeName => $"{Name} {{{TypeName}}}";

    public virtual int GetChildIndex(FlatBufferNode? child) => -1;

    /// <summary>
    /// Override the local type with a shared type
    /// </summary>
    /// <param name="type">The shared FBType</param>
    public virtual void TrackType(FBType type)
    {
        FieldInfo = FieldInfo with { Type = type };
    }

    /// <summary>
    /// Override the local field info with a shared field
    /// </summary>
    /// <param name="sharedInfo">The shared FBFieldInfo</param>
    public virtual void TrackFieldInfo(FBFieldInfo sharedInfo)
    {
        FieldInfo = sharedInfo;
    }
}
