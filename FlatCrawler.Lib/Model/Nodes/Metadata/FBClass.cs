using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FlatCrawler.Lib;

public sealed record FBClass() : FBType(TypeCode.Object)
{
    public event EventHandler<MemberTypeChangedArgs>? MemberTypeChanged;
    public event EventHandler<int>? MemberCountChanged;

    private FBFieldInfo[] _members = Array.Empty<FBFieldInfo>();

    public IReadOnlyList<FBFieldInfo> Members => _members;

    private readonly SortedDictionary<int, VTable> AssociatedVTables = new();

    public int GetMemberIndex(int offset)
    {
        if (offset == 0)
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Value of 0 for offset is not valid (Default Value)");

        var index = Array.FindIndex(_members, z => z.Offset == offset);
        if (index == -1)
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Unable to find the member index with that offset.");
        var last = Array.FindLastIndex(_members, z => z.Offset == offset);
        if (last != index)
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"More than one member has that offset: {index}, {last}");

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

        _members[memberIndex] = member;

        OnMemberTypeChanged(new(memberIndex, member, oldType, member.Type));
    }

    public void AssociateVTable(VTable vtable)
    {
        if (AssociatedVTables.ContainsKey(vtable.Location))
            return;

        AssociatedVTables.Add(vtable.Location, vtable);

        // Append members until count matches vtable field count
        var info = vtable.FieldInfo;
        if (_members.Length < info.Length)
        {
            var newMembers = new FBFieldInfo[info.Length];
            _members.CopyTo(newMembers, 0);
            for (int i = _members.Length; i < info.Length; ++i)
            {
                var field = info[i];
                newMembers[i] = new FBFieldInfo { Offset = field.Offset, Size = field.Size };
            }
            _members = newMembers;

            OnMemberCountChanged();
        }

        UpdateOffsets(vtable);
        UpdateSizes(vtable);
    }

    private void UpdateOffsets(VTable vtable)
    {
        // Overwrite any zero offsets
        var fi = vtable.FieldInfo;
        for (int i = 0; i < fi.Length; ++i)
        {
            var member = _members[i];
            if (member.Offset != 0)
                continue;

            _members[i] = member with { Offset = fi[i].Offset };
        }
    }

    private void UpdateSizes(VTable vtable)
    {
        // Update type size
        Size = Math.Max(Size, vtable.DataTableLength);

        // Code below is mainly there for verification of VTable sizes

        // Loop in reverse order, starting at the table size
        // Field size would be Start byte - End byte.
        // Eg. 12 (table length) - 8 (offset) = size of 4 bytes
        // Next field would end at 8

        // Store index and offset in reverse order
        var sortedFields = _members
            .Select((x, Index) => new { x.Offset, Index })
            .Where(x => x.Offset != 0) // Zero offsets don't exist in the table so have no size
            .OrderByDescending(z => z.Offset)
            .ToArray();

        int end = Size;
        foreach (var f in sortedFields)
        {
            ref var member = ref _members[f.Index];

            var start = f.Offset;
            var delta = end - start;
            if (member.Size != delta)
                Debug.WriteLine("Found a size that was incorrectly calculated in the VTable. Should probably update it there too.");

            member = member with { Size = delta };
            end = start;
        }
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
