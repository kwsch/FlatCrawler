using System;
using System.Text;

namespace FlatCrawler.Lib;

public sealed record FieldTypeTracker
{
    public FieldType Type { get; private set; } = FieldType.Unknown;
    public uint Single { get; private set; }
    public uint Array { get; private set; }

    private bool FirstPass { get; set; } = true;

    public void PreCheck(FieldSizeTracker size) => Type = size.GuessOverallType();

    public void Observe(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data, FieldSizeTracker sizes)
    {
        if (FirstPass)
            ObserveFirst(entry, index, data, sizes);
        else
            ObserveSubsequent(entry, index, data);
    }

    private void ObserveFirst(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data, FieldSizeTracker sizes)
    {
        FirstPass = false;
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
        if (Type.HasFlagFast(FieldType.StructArray))
        {
            foreach (var (type, size) in Structs)
            {
                if (sizes.IsPlausible(size))
                    Array |= TryReadTable(entry, index, data, type);
            }
        }

        if (sizes.IsPlausible(4))
        {
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
    }

    private void ObserveSubsequent(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data)
    {
        foreach (var (type, _) in Structs)
        {
            var mask = 1u << (int)type;
            if ((Single & mask) != 0)
            {
                if (TryRead(entry, index, data, type) == 0)
                    Single &= ~mask;
            }
            if ((Array & mask) != 0)
            {
                if (TryRead(entry, index, data, type) == 0)
                    Array &= ~mask;
            }
        }
        {
            const uint mask = 1u << (int)TypeCode.Boolean;
            if ((Single & mask) != 0)
            {
                if (TryReadBoolean(entry, index, data) == 0)
                    Single &= ~mask;
            }
        }
        if (Type.HasFlagFast(FieldType.Object))
        {
            if (TryRead(entry, index, data, TypeCode.Object) == 0)
                Single &= ~(1u << (int)TypeCode.Object);
            if (TryRead(entry, index, data, TypeCode.String) == 0)
                Single &= ~(1u << (int)TypeCode.String);
        }
        if (Type.HasFlagFast(FieldType.ObjectArray))
        {
            if (TryReadTable(entry, index, data, TypeCode.Object) == 0)
                Array &= ~(1u << (int)TypeCode.Object);
            if (TryReadTable(entry, index, data, TypeCode.String) == 0)
                Array &= ~(1u << (int)TypeCode.String);
        }
    }

    private static uint TryReadTable(FlatBufferNodeField entry, int index, ReadOnlySpan<byte> data, TypeCode type)
    {
        try
        {
            _ = entry.ReadArrayAs(data, index, type);
            return 1u << (int)type;
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
            _ = entry.ReadAs(data, index, type);
            return 1u << (int)type;
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
