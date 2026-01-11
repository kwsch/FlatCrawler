using System;
using System.Runtime.CompilerServices;
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

    /// <summary> Indicates that the field can potentially be interpreted as a <see cref="FlatBufferObject"/>. </summary>
    public bool IsPotentialObject => (Single & (1u << (int)TypeCode.Object)) != 0;

    /// <summary> Indicates that the field can be potentially interpreted as a <see cref="FlatBufferTableObject"/> </summary>
    public bool IsPotentialObjectArray => (Array & (1u << (int)TypeCode.Object)) != 0;

    /// <summary>
    /// Indicates if the field's type can be any of the recognized types.
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
        if (Type.HasFlag(FieldType.StructSingle))
        {
            foreach (var (type, size) in Structs)
            {
                if (sizes.IsPlausible(size))
                    Single |= TryRead(entry, index, data, type, size);
            }
            if (sizes.IsPlausible(1))
                Single |= TryReadBoolean(entry, index, data);
        }

        if (!sizes.IsPlausible(4))
            return;

        if (Type.HasFlag(FieldType.StructArray))
        {
            foreach (var (type, _) in Structs)
                Array |= TryReadTable(entry, index, data, type);
        }

        if (Type.HasFlag(FieldType.Object))
        {
            Single |= TryRead(entry, index, data, TypeCode.Object, 4);
            Single |= TryRead(entry, index, data, TypeCode.String, 4);
        }

        if (Type.HasFlag(FieldType.ObjectArray))
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
        foreach (var (type, size) in Structs)
        {
            var mask = 1u << (int)type;
            if ((Single & mask) != 0 && TryRead(entry, index, data, type, size) == 0)
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
            const uint mask = 1u << (int)TypeCode.Object;
            if ((Single & mask) != 0 && TryRead(entry, index, data, TypeCode.Object, 4) == 0)
                Single &= ~mask;
            if ((Array & mask) != 0 && TryReadTable(entry, index, data, TypeCode.Object) == 0)
                Array &= ~mask;
        }
        {
            const uint mask = 1u << (int)TypeCode.String;
            if ((Single & mask) != 0 && TryRead(entry, index, data, TypeCode.String, 4) == 0)
                Single &= ~mask;
            if ((Array & mask) != 0 && TryReadTable(entry, index, data, TypeCode.String) == 0)
                Array &= ~mask;
        }
    }

    private static uint TryReadTable(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data, TypeCode type)
    {
        try
        {
            bool potential = IsPotentialArray(entry, index, data, type);
            if (!potential)
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

    public static bool IsPotentialSingle(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data, TypeCode type, int size)
    {
        if (type is TypeCode.Object or TypeCode.String)
        {
            var reference = entry.GetReferenceOffset(index, data);
            return (uint)reference < data.Length - 4;
        }

        var field = entry.GetFieldOffset(index);
        return (uint)field <= data.Length - size;
    }

    public static bool IsPotentialArray(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data, TypeCode type)
    {
        var sizeCheck = entry.PeekArraySize(data, index, type);
        return (ulong)(sizeCheck - 4) <= (ulong)data.Length;
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

    private static uint TryRead(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data, TypeCode type, int size)
    {
        try
        {
            if (!IsPotentialSingle(entry, index, data, type, size))
                return 0;

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
    [
        (TypeCode.Byte, 1),
        (TypeCode.UInt16, 2),
        (TypeCode.UInt32, 4),
        (TypeCode.Single, 4),
        (TypeCode.UInt64, 8),
        (TypeCode.Double, 8),
    ];

    public string Summary()
    {
        if (!IsRecognized)
            return "Unrecognized";

        var sb = new StringBuilder();
        if (Single != 0)
        {
            sb.Append("Possible Types: ");
            foreach (var (type, _) in Structs)
                AppendIfSet(Single, type, sb, false);
            AppendIfSet(Single, TypeCode.Boolean, sb, false);
            AppendIfSet(Single, TypeCode.Object, sb, false);
            AppendIfSet(Single, TypeCode.String, sb, false);
        }
        if (Array != 0)
        {
            if (Single != 0)
                sb.Append("    ");
            sb.Append("Possible ArrayTypes: ");
            foreach (var (type, _) in Structs)
                AppendIfSet(Array, type, sb, true);
            AppendIfSet(Array, TypeCode.Object, sb, true);
            AppendIfSet(Array, TypeCode.String, sb, true);
        }
        return sb.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendIfSet(uint flags, TypeCode type, StringBuilder sb, bool array)
    {
        if ((flags & (1u << (int)type)) == 0)
            return;
        sb.Append(type.ToTypeString()).Append(array ? "[]" : "").Append("; ");
    }

    public string Summary(FlatBufferNodeField node, int index, ReadOnlySpan<byte> data)
    {
        if (!IsRecognized)
            return "Unrecognized";

        // For each bitflag that is set, print the type and the value by reading the node.
        var sb = new StringBuilder();
        if (Single != 0)
        {
            sb.Append("Possible Types: ");
            foreach (var (type, _) in Structs)
                AppendIfSet(Single, type, node, index, data, sb, false);
            AppendIfSet(Single, TypeCode.Boolean, node, index, data, sb, false);
            AppendIfSet(Single, TypeCode.Object, node, index, data, sb, false);
            AppendIfSet(Single, TypeCode.String, node, index, data, sb, false);
        }
        if (Array != 0)
        {
            if (Single != 0)
                sb.Append("    ");
            sb.Append("Possible ArrayTypes: ");
            foreach (var (type, _) in Structs)
                AppendIfSet(Array, type, node, index, data, sb, true);
            AppendIfSet(Array, TypeCode.Object, node, index, data, sb, true);
            AppendIfSet(Array, TypeCode.String, node, index, data, sb, true);
        }
        return sb.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendIfSet(uint flags, TypeCode type, FlatBufferNodeField node, int index, ReadOnlySpan<byte> data, StringBuilder sb, bool array)
    {
        if ((flags & (1u << (int)type)) == 0)
            return;
        try
        {
            var display = GetDisplayValue(node, index, data, type, array);
            sb.Append(display).Append("; ");
        }
        catch
        {
            /* Ignore */
        }
    }

    public static string GetDisplayValue(FlatBufferNodeField node, int index, ReadOnlySpan<byte> data, TypeCode type, bool array)
    {
        if (!array)
        {
            if (!node.HasField(index))
                return $"{type.ToTypeString()} (Default)";
            if (type is TypeCode.Object)
                return node.ReadAsObject(data, index).TypeName;
            return node.ReadAs(data, index, type).TypeName;
        }
        // array
        {
            if (!node.HasField(index))
                return $"{type.ToTypeString()}[] (null)";
            if (type is TypeCode.Object && node.ReadAsTable(data, index) is { } obj)
                return $"{obj.TypeName}";
            if (type is TypeCode.String && node.ReadAsStringTable(data, index) is { } str)
                return $"string[{str.Entries.Length}]";
            if (node.ReadArrayAs(data, index, type) is IArrayNode a)
                return $"{type.ToTypeString()}[{a.Entries.Count}]";
        }
        throw new ArgumentException("Unrecognized type.", nameof(type));
    }
}
