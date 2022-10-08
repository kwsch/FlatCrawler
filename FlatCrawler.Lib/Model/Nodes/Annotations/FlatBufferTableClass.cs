using System;
using System.Diagnostics.CodeAnalysis;

namespace FlatCrawler.Lib;

public sealed record FlatBufferTableClass
{
    public event EventHandler<MemberTypeChangedArgs>? MemberTypeChanged;
    private readonly FlatBufferNodeType[] MemberTypes;
    private readonly FlatBufferTableClass?[] SubClasses;

    public FlatBufferTableClass(int fieldCount)
    {
        MemberTypes = new FlatBufferNodeType[fieldCount];
        SubClasses = new FlatBufferTableClass?[fieldCount];
    }

    #region SubClass

    public bool TryGetSubClass(int memberIndex, [NotNullWhen(true)] out FlatBufferTableClass? subClass)
    {
        if ((uint)memberIndex >= SubClasses.Length)
        {
            subClass = null;
            return false;
        }
        subClass = SubClasses[memberIndex];
        return subClass != null;
    }

    public bool HasSubClass(int memberIndex) => SubClasses[memberIndex] != null;
    public FlatBufferTableClass? GetSubClass(int memberIndex) => SubClasses[memberIndex];
    public FlatBufferTableClass RegisterSubClass(int memberIndex, int subClassFieldCount)
    {
        if (TryGetSubClass(memberIndex, out var subClass))
            return subClass;

        var type = new FlatBufferTableClass(subClassFieldCount);
        MemberTypes[memberIndex] = new(TypeCode.Object, true);
        SubClasses[memberIndex] = type;
        return type;
    }

    #endregion

    #region MemberType

    public bool HasMemberType(int memberIndex) => !MemberTypes[memberIndex].IsDefined;
    public FlatBufferNodeType? GetMemberType(int memberIndex) => MemberTypes[memberIndex];
    public void SetMemberType(int memberIndex, TypeCode type, bool asArray)
    {
        var oldType = MemberTypes[memberIndex];
        var newType = new FlatBufferNodeType(type, asArray);
        MemberTypes[memberIndex] = newType;
        OnMemberTypeChanged(new(memberIndex, oldType, newType));
    }

    private void OnMemberTypeChanged(MemberTypeChangedArgs args)
    {
        MemberTypeChanged?.Invoke(this, args);
    }

    #endregion
}
