using System;

namespace FlatCrawler.Lib;

/// <summary>
/// Base FlatBuffer node that is inherited by more specific node types.
/// </summary>
/// <param name="Offset">Absolute Offset of the node from the start of the FlatBuffer file.</param>
/// <param name="Parent">Parent that owns this child node.</param>
public abstract record FlatBufferNode(int Offset, FlatBufferNode? Parent = null)
{
    public virtual FlatBufferFile FbFile => Parent?.FbFile ?? throw new InvalidOperationException($"{nameof(FlatBufferNode)} is not attached to a {nameof(FlatBufferFile)}");

    /// <summary> Parent that owns this child node. </summary>
    public readonly FlatBufferNode? Parent = Parent;

    /// <summary> Field Info for this node. </summary>
    public FBFieldInfo FieldInfo { get; protected set; } = new();

    /// <summary> Absolute offset that the node starts at. </summary>
    public readonly int Offset = Offset;

    /// <summary>
    /// The size of the node in bytes
    /// </summary>
    protected int Size => FieldInfo.Size;

    /// <summary> Tagged name of the node. </summary>
    public virtual string Name { get => FieldInfo.Name; set => FieldInfo.Name = value; }

    /// <summary> Schema class name of the node. For struct type nodes, this is the numeric type name and its display value. </summary>
    public virtual string TypeName { get => FieldInfo.Type.TypeName; set => FieldInfo.Type.TypeName = value; }

    /// <summary> Friendly name to display as the node name. </summary>
    public string FullNodeName => $"{Name} {{{TypeName}}}";

    /// <summary>
    /// Checks if the input <see cref="child"/> is owned by this parent node.
    /// </summary>
    /// <param name="child">Child node to check ownership of</param>
    /// <returns>-1 if not a child, otherwise the array/field index of the child.</returns>
    public virtual int GetChildIndex(FlatBufferNode? child) => -1;

    /// <summary>
    /// Override the local type with a shared type
    /// </summary>
    /// <param name="sharedInfo">The shared field information</param>
    public virtual void TrackType(FBFieldInfo sharedInfo) => FieldInfo = FieldInfo with { Type = sharedInfo.Type, Size = sharedInfo.Size };

    /// <summary>
    /// Override the local field info with a shared field
    /// </summary>
    /// <param name="sharedInfo">The shared field information</param>
    public virtual void TrackFieldInfo(FBFieldInfo sharedInfo) => FieldInfo = sharedInfo;

    public virtual void RegisterMemory() { }
    public virtual void UnRegisterMemory() { }
}
