using System;
using System.Text;

namespace FlatCrawler.Lib;

/// <summary>
/// Tracks the possible types of a field.
/// </summary>
public sealed record FieldTypeTracker
{
    /// <summary> Potential field type groupings. </summary>
    private FieldType Type { get; set; } = FieldType.Unknown;

    /// <summary> Tracks how many times the field has been observed from a provided <see cref="FlatBufferNodeField"/>. </summary>
    public int Observations { get; private set; }

    /// <summary> Packed bits of <see cref="TypeCode"/> values (flags), indicating if the field can be interpreted as a single value (not array) with that type. </summary>
    public uint Single { get; private set; }

    /// <summary> Packed bits of <see cref="TypeCode"/> values (flags), indicating if the field can be interpreted as an array with that type. </summary>
    public uint Array { get; private set; }

    /// <summary> Indicates that the field can potentially be interpreted as an <see cref="FlatBufferObject"/>. </summary>
    public bool IsPotentialObject => (Single & (1u << (int)TypeCode.Object)) != 0;

    /// <summary> Indicates that the field can be potentially interpreted as an <see cref="FlatBufferTableObject"/> </summary>
    public bool IsPotentialObjectArray => (Array & (1u << (int)TypeCode.Object)) != 0;

    /// <summary>
    /// Indicates if the field's type can be one of any of the recognized types.
    /// </summary>
    public bool IsRecognized => Single != 0 || Array != 0;

    /// <summary>
    /// Determines the overall <see cref="FieldType"/> values the field could potentially be, before attempting to interpret.
    /// </summary>
    /// <param name="size"></param>
    public void PreCheck(FieldSizeTracker size) => Type = size.GuessOverallType();

    /// <summary>
    /// Attempts to interpret the field, and removes all type flags that are not compatible with the interpretation.
    /// </summary>
    public void Observe(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data, FieldSizeTracker sizes)
    {
        // If this is our first time looking at the node, we have not yet populated our type flags.
        // We can do this by looking at the size of the field, and determining what types it could be.
        // This first pass is only done once for a type; future passes will remove flags if the type is not compatible.
        if (Observations++ == 0)
            ObserveFirst(entry, index, data, sizes);
        else
            ObserveSubsequent(entry, index, data);
    }

    private void ObserveFirst(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data, FieldSizeTracker sizes)
    {
        // The potential type flags have not been populated yet.
        // We need to bitwise-or them if the field can be interpreted as any of the types.
        if (Type.HasFlagFast(FieldType.StructSingle))
        {
            foreach (var (type, size) in Structs)
            {
                if (sizes.IsPlausible(size))
                    Single |= TryRead(entry, index, data, type);
            }
            if (sizes.IsPlausible(1))
                Single |= TryReadBoolean(entry, index, data);
        }

        if (!sizes.IsPlausible(4))
            return;

        if (Type.HasFlagFast(FieldType.StructArray))
        {
            foreach (var (type, _) in Structs)
                Array |= TryReadTable(entry, index, data, type);
        }

        if (Type.HasFlagFast(FieldType.Object))
        {
            Single |= TryRead(entry, index, data, TypeCode.Object);
            Single |= TryRead(entry, index, data, TypeCode.String);
        }

        if (Type.HasFlagFast(FieldType.ObjectArray))
        {
            Array |= TryReadTable(entry, index, data, TypeCode.Object);
            Array |= TryReadTable(entry, index, data, TypeCode.String);
        }
    }

    private void ObserveSubsequent(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data)
    {
        // The potential type flags have already been populated.
        // If the field cannot be interpreted as any of the flagged types, remove the flag.
        // Only check flagged types.
        foreach (var (type, _) in Structs)
        {
            var mask = 1u << (int)type;
            if ((Single & mask) != 0 && TryRead(entry, index, data, type) == 0)
                Single &= ~mask;
            if ((Array & mask) != 0 && TryReadTable(entry, index, data, type) == 0)
                Array &= ~mask;
        }
        {
            const uint mask = 1u << (int)TypeCode.Boolean;
            if ((Single & mask) != 0 && TryReadBoolean(entry, index, data) == 0)
                Single &= ~mask;
        }
        {
            const uint om = 1u << (int)TypeCode.Object;
            if ((Single & om) != 0 && TryRead(entry, index, data, TypeCode.Object) == 0)
                Single &= ~om;
            if ((Array & om) != 0 && TryReadTable(entry, index, data, TypeCode.Object) == 0)
                Array &= ~om;
        }
        {
            const uint mask = 1u << (int)TypeCode.String;
            if ((Single & mask) != 0 && TryRead(entry, index, data, TypeCode.String) == 0)
                Single &= ~mask;
            if ((Array & mask) != 0 && TryReadTable(entry, index, data, TypeCode.String) == 0)
                Array &= ~mask;
        }
    }

    private static uint TryReadTable(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data, TypeCode type)
    {
        try
        {
            var sizeCheck = entry.PeekArraySize(data, index, type);
            if ((ulong)sizeCheck >= (ulong)data.Length)
                return 0;

            var child = entry.ReadArrayAs(data, index, type);
            return child switch
            {
                FlatBufferTableString { IsReadable: false } => 0,
                _ => 1u << (int)type,
            };
        }
        catch
        {
            return 0;
        }
    }

    private static uint TryReadBoolean(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data)
    {
        try
        {
            var child = entry.ReadAs<byte>(data, index);
            if (child.Value is 0 or 1)
                return 1u << (int)TypeCode.Boolean;
        }
        catch { /* Ignore */ }
        return 0;
    }

    private static uint TryRead(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data, TypeCode type)
    {
        try
        {
            var child = entry.ReadAs(data, index, type);
            return child switch
            {
                FlatBufferStringValue { IsReadable: false } => 0,
                _ => 1u << (int)type,
            };
        }
        catch
        {
            return 0;
        }
    }

    private static readonly (TypeCode Type, int Size)[] Structs =
    {
        (TypeCode.Byte, 1),
        (TypeCode.UInt16, 2),
        (TypeCode.UInt32, 4),
        (TypeCode.Single, 4),
        (TypeCode.UInt64, 8),
        (TypeCode.Double, 8),
    };

    public string Summary()
    {
        var sb = new StringBuilder();
        if (Single != 0)
        {
            sb.Append("Type: ");
            foreach (var (type, _) in Structs)
            {
                var mask = 1u << (int)type;
                if ((Single & mask) != 0)
                    sb.Append(type).Append(' ');
            }
            if ((Single & (1u << (int)TypeCode.Boolean)) != 0)
                sb.Append(TypeCode.Boolean).Append(' ');
            if ((Single & (1u << (int)TypeCode.Object)) != 0)
                sb.Append(TypeCode.Object).Append(' ');
            if ((Single & (1u << (int)TypeCode.String)) != 0)
                sb.Append(TypeCode.String).Append(' ');
        }

        if (Array != 0)
        {
            if (Single != 0)
                sb.Append("    ");
            sb.Append("ArrayType: ");
            foreach (var (type, _) in Structs)
            {
                var mask = 1u << (int)type;
                if ((Array & mask) != 0)
                    sb.Append(type).Append(' ');
            }
            if ((Array & (1u << (int)TypeCode.Object)) != 0)
                sb.Append(TypeCode.Object).Append(' ');
            if ((Array & (1u << (int)TypeCode.String)) != 0)
                sb.Append(TypeCode.String).Append(' ');
        }
        return sb.ToString();
    }

    public string Summary(FlatBufferNodeField node, int index, ReadOnlySpan<byte> data)
    {
        // For each bitflag that is set, print the type and the value by reading the node.
        var sb = new StringBuilder();
        if (Single != 0)
        {
            sb.Append("Type: ");
            foreach (var (type, _) in Structs)
            {
                var mask = 1u << (int)type;
                if ((Single & mask) != 0)
                {
                    try
                    {
                        var name = node.ReadAs(data, index, type);
                        var value = name.TypeName;
                        sb.Append(value).Append(' ');
                    }
                    catch { /* Ignore */ }
                }
            }
            if ((Single & (1u << (int)TypeCode.Boolean)) != 0)
            {
                sb.Append(TypeCode.Boolean).Append(' ');
                sb.Append(node.ReadAs<bool>(data, index).Value).Append(' ');
            }
            if ((Single & (1u << (int)TypeCode.Object)) != 0)
            {
                try
                {
                    var value = node.ReadAsObject(data, index).TypeName;
                    sb.Append(TypeCode.Object).Append(' ');
                    sb.Append(value).Append(' ');
                }
                catch { /* Ignore */ }
            }
            if ((Single & (1u << (int)TypeCode.String)) != 0)
            {
                try
                {
                    var value = node.ReadAsString(data, index).TypeName;
                    sb.Append(TypeCode.String).Append(' ');
                    sb.Append(value).Append(' ');
                }
                catch { /* Ignore */ }
            }
        }

        if (Array != 0)
        {
            if (Single != 0)
                sb.Append("    ");
            sb.Append("ArrayType: ");
            foreach (var (type, _) in Structs)
            {
                var mask = 1u << (int)type;
                if ((Array & mask) != 0)
                {
                    sb.Append(type).Append('[');
                    sb.Append(((IArrayNode)node.ReadArrayAs(data, index, type)).Entries.Count).Append("] ");
                }
            }
            if ((Array & (1u << (int)TypeCode.Object)) != 0)
            {
                try
                {
                    var length = node.ReadAsTable(data, index).Entries.Length;
                    sb.Append(TypeCode.Object);
                    sb.Append('[').Append(length).Append("] ");
                }
                catch { /* Ignore */ }
            }
            if ((Array & (1u << (int)TypeCode.String)) != 0)
            {
                try
                {
                    var length = node.ReadAsStringTable(data, index).Entries.Length;
                    sb.Append(TypeCode.String);
                    sb.Append('[').Append(length).Append("] ");
                }
                catch { /* Ignore */ }
            }
        }
        return sb.ToString();
    }
}
