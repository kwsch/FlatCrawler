using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace FlatCrawler.Lib;

public sealed record FBClass : FBType
{
    public event EventHandler<MemberTypeChangedArgs>? MemberTypeChanged;
    public event EventHandler<int>? MemberCountChanged;

    private FBFieldInfo[] _members = Array.Empty<FBFieldInfo>();

    public IReadOnlyList<FBFieldInfo> Members => _members;

    public FBClass() :
        base(TypeCode.Object)
    { }

    public int GetMemberIndex(int offset)
    {
        if (offset == 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Value of 0 for offset is not valid (Default Value)");

        var index = _members
            .Select((Field, Index) => new { Field, Index })
            .SingleOrDefault(x => x.Field.OffsetInVTable == offset)
            ?.Index ?? -1;

        if (index == -1)
            throw new ArgumentOutOfRangeException(nameof(offset), "Unable to find the member index with that offset.");

        return index;
    }

    public void SetMemberType(int memberIndex, TypeCode type, bool asArray = false)
    {
        var member = _members[memberIndex];
        var oldType = member.Type;
        if (oldType.Type == type && member.IsArray == asArray)
            return;

        if (type == TypeCode.Object)
            member = (member with { Type = new FBClass(), IsArray = asArray });
        else
            member = (member with { Type = new FBType(type), IsArray = asArray });

        OnMemberTypeChanged(new(memberIndex, member, oldType, member.Type));
    }


    public void AssociateVTable(VTable vtable)
    {
        UpdateOffsets(vtable);
        UpdateSizes(vtable);
    }

    private void UpdateOffsets(VTable vtable)
    {
        // Find the offsets that are not part of the _members list and add them
        var vTableOffsets = vtable.FieldInfo.Select(x => x.Offset).Where(x => x != 0);
        var trackedOffsets = _members.Select(x => x.OffsetInVTable);

        var untrackedOffsets = vTableOffsets.Except(trackedOffsets);

        if (untrackedOffsets.Any())
        {
            // Try to preserve field references 
            var updatedMembers = _members.AsEnumerable();
            foreach (var offset in untrackedOffsets)
                updatedMembers = updatedMembers.Append(new FBFieldInfo { OffsetInVTable = offset });

            _members = updatedMembers.OrderBy(x => x.OffsetInVTable).ToArray();

            OnMemberCountChanged();
        }
    }

    private void UpdateSizes(VTable vtable)
    {
    }

    private void OnMemberTypeChanged(MemberTypeChangedArgs args)
    {
        MemberTypeChanged?.Invoke(this, args);
    }

    private void OnMemberCountChanged()
    {
        MemberCountChanged?.Invoke(this, Members.Count);
    }
}
