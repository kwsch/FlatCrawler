using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FlatCrawler.Lib;

public sealed record FBClass() : FBType(TypeCode.Object)
{
    public readonly List<ISchemaObserver> Observers = new();

    private FBFieldInfo[] _members = Array.Empty<FBFieldInfo>();

    public IReadOnlyList<FBFieldInfo> Members => _members;

    private readonly SortedDictionary<int, VTable> AssociatedVTables = new();

    public void SetMemberType(int memberIndex, ReadOnlySpan<byte> data, TypeCode type, bool asArray = false)
    {
        ref var member = ref _members[memberIndex];
        var oldType = member.Type;
        if (member.HasShape(type, asArray))
            return; // already the requested type, no modification needed

        if (type == TypeCode.Object)
            member = member with { IsArray = asArray, Type = new FBClass() };
        else
            member = member with { IsArray = asArray, Type = new FBType(type) };

        OnMemberTypeChanged(new(memberIndex, data, member, oldType, member.Type));
    }

    public ClassUpdateResult AssociateVTable(VTable vtable)
    {
        ClassUpdateResult result = 0;
        if (AssociatedVTables.ContainsKey(vtable.Location))
            return ClassUpdateResult.None;

        AssociatedVTables.Add(vtable.Location, vtable);

        // We can't map to VTable offsets here, as each VTable can point to different offsets.
        // The only guarantee we have is that they map to the correct field id
        // A table field id maps to a vtable field id using the formula
        // sizeof(ushort) * (field-id + 2)

        // The class size is the vtable with the largest object size
        // Object size is calculated by taking the data table size and subtracting the offset of the first field [0] if not 0

        // The amount of class members is equal to the vtable with the largest field count

        // Field sizes are known unless a field between two known fields has a default value
        // This can be resolved by taking the vtable that calculated the smallest size for the field

        // TODO: Fix this later, don't have time right now :)

        // var objectSize = vtable.DataTableLength - vtable.FieldInfo[0].Offset;
        // var maxFields = 0;

        // Append members until count matches vtable field count
        var info = vtable.FieldInfo;
        if (_members.Length < info.Length)
        {
            var newMembers = new FBFieldInfo[info.Length];
            _members.CopyTo(newMembers, 0);
            for (int i = info.Length; i < info.Length; i++)
            {
                var field = info[i];
                newMembers[i] = new FBFieldInfo { Size = field.Size };
            }
            _members = newMembers;
            result |= ClassUpdateResult.UpdatedCount;
            OnMemberCountChanged();
        }

        result |= UpdateSizes(vtable);
        return result;
    }

    private ClassUpdateResult UpdateSizes(VTable vtable)
    {
        ClassUpdateResult result = 0;

        // Update type size
        if (Size < vtable.DataTableLength)
        {
            Size = vtable.DataTableLength;
            result |= ClassUpdateResult.UpdatedSelfTotal;
        }

        // Code below is mainly there for verification of VTable sizes

        // Loop in reverse order, starting at the table size
        // Field size would be Start byte - End byte.
        // Eg. 12 (table length) - 8 (offset) = size of 4 bytes
        // Next field would end at 8

        // Store index and offset in reverse order
        var sortedFields = vtable.FieldInfo.GetOrderedList();

        int end = Size;
        foreach ((int offset, int index) in sortedFields)
        {
            ref var member = ref _members[index];

            var size = end - offset;
            if (member.Size > size)
            {
                // update our size
                Debug.WriteLine($"Found a size for field {index} that is smaller in the provided VTable. Updating class info.");
                member = member with { Size = size };
                result |= ClassUpdateResult.UpdatedSelf;
            }
            else if (member.Size < size)
            {
                // Update VTable size
                Debug.WriteLine($"Class info for field {index} is smaller than the provided VTable. Updating VTable size.");
                vtable.FieldInfo[index] = vtable.FieldInfo[index] with { Size = size };
                result |= ClassUpdateResult.UpdatedVTable;
            }
            end = offset;
        }
        return result;
    }

    private void OnMemberTypeChanged(MemberTypeChangedArgs args)
    {
        foreach (var subscriber in Observers)
            subscriber.OnMemberTypeChanged(args);
    }

    private void OnMemberCountChanged()
    {
        foreach (var subscriber in Observers)
            subscriber.OnMemberCountChanged(Members.Count);
    }
}

public interface ISchemaObserver
{
    void OnMemberTypeChanged(MemberTypeChangedArgs args);
    void OnMemberCountChanged(int count);
}

[Flags]
public enum ClassUpdateResult
{
    None = 0,
    UpdatedCount = 1 << 0,
    UpdatedSelfTotal = 1 << 1,
    UpdatedSelf = 1 << 2,
    UpdatedVTable = 1 << 3,
}
