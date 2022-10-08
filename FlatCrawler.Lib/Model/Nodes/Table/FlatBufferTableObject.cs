using System;
using System.Linq;

namespace FlatCrawler.Lib
{
    public sealed record MemberTypeChangedArgs
    {
        public int MemberIndex { get; init; }
        public FlatBufferNodeType OldType { get; init; }
        public FlatBufferNodeType NewType { get; init; }

        public MemberTypeChangedArgs(int memberIndex, FlatBufferNodeType oldType, FlatBufferNodeType newType)
        {
            MemberIndex = memberIndex;
            OldType = oldType;
            NewType = newType;
        }
    }

    public sealed record FlatBufferTableClass
    {
        public event EventHandler<MemberTypeChangedArgs>? MemberTypeChanged;

        private readonly FlatBufferNodeType[] MemberTypes;

        private readonly FlatBufferTableClass[] SubClasses;

        public FlatBufferTableClass(int fieldCount)
        {
            MemberTypes = new FlatBufferNodeType[fieldCount];
            SubClasses = new FlatBufferTableClass[fieldCount];
        }

        public bool HasSubClass(int memberIndex) => SubClasses[memberIndex] != null;
        public FlatBufferTableClass GetSubClass(int memberIndex) => SubClasses[memberIndex];
        public void RegisterSubClass(int memberIndex, int subClassFieldCount)
        {
            if (SubClasses[memberIndex] != null)
                return;

            MemberTypes[memberIndex] = new(TypeCode.Object, true);
            SubClasses[memberIndex] = new FlatBufferTableClass(subClassFieldCount);
        }

        public bool HasMemberType(int memberIndex) => MemberTypes[memberIndex] != null;
        public FlatBufferNodeType GetMemberType(int memberIndex) => MemberTypes[memberIndex];
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
    }

    public sealed record FlatBufferTableObject : FlatBufferTable<FlatBufferObject>
    {
        public const int HeaderSize = 4;
        public const int EntrySize = 4;

        private string _name = "???";
        private string _typeName = "Object[]";
        public override string TypeName
        {
            get => _typeName;
            set
            {
                _typeName = value + "[]";
                foreach (var e in Entries)
                {
                    e.TypeName = value;
                }
            }
        }

        public override string Name
        {
            get => _name;
            set
            {
                _name = value;
                for (int i = 0; i < Entries.Length; ++i)
                {
                    Entries[i].Name = $"{value}[{i}]";
                }
            }
        }

        private FlatBufferTableClass ObjectClass = null!; // will always be set by static initializer

        public override FlatBufferObject GetEntry(int entryIndex) => Entries[entryIndex];

        private FlatBufferTableObject(int offset, int length, FlatBufferNode parent, int dataTableOffset) :
            base(offset, parent, length, dataTableOffset)
        { }

        private void ReadArray(byte[] data)
        {
            for (int i = 0; i < Entries.Length; i++)
                Entries[i] = GetEntryAtIndex(data, i);
        }

        private FlatBufferObject GetEntryAtIndex(byte[] data, int entryIndex)
        {
            var arrayEntryPointerOffset = DataTableOffset + (entryIndex * EntrySize);
            var dataTablePointerShift = BitConverter.ToInt32(data, arrayEntryPointerOffset);
            var dataTableOffset = arrayEntryPointerOffset + dataTablePointerShift;

            return FlatBufferObject.Read(arrayEntryPointerOffset, this, data, dataTableOffset);
        }

        public static FlatBufferTableObject Read(int offset, FlatBufferNode parent, int fieldIndex, byte[] data)
        {
            int length = BitConverter.ToInt32(data, offset);
            var node = new FlatBufferTableObject(offset, length, parent, offset + HeaderSize);
            node.ReadArray(data);

            // If this table is part of another table, link the ObjectTypes (class) reference
            if (node.Parent?.Parent is FlatBufferTableObject t)
            {
                if (!t.ObjectClass.HasSubClass(fieldIndex))
                {
                    int memberCount = node.Entries.Select(x => x.VTable.FieldInfo.Length).Max();
                    t.ObjectClass.RegisterSubClass(fieldIndex, memberCount);
                }

                node.ObjectClass = t.ObjectClass.GetSubClass(fieldIndex);
            }
            else
            {
                int memberCount = node.Entries.Select(x => x.VTable.FieldInfo.Length).Max();
                node.ObjectClass = new FlatBufferTableClass(memberCount);
            }

            node.ObjectClass.MemberTypeChanged += node.ObjectClass_MemberTypeChanged;

            return node;
        }

        public static FlatBufferTableObject Read(FlatBufferNodeField parent, int fieldIndex, byte[] data) => Read(parent.GetReferenceOffset(fieldIndex, data), parent, fieldIndex, data);

        private void ObjectClass_MemberTypeChanged(object? sender, MemberTypeChangedArgs e)
        {
            foreach (var entry in Entries)
            {
                if (entry.HasField(e.MemberIndex))
                    entry.UpdateNodeType(e.MemberIndex, CommandUtil.Data.ToArray(), e.NewType.Type, e.NewType.IsArray);
            }
        }

        public void OnFieldTypeChanged(int fieldIndex, TypeCode code, bool asArray, FlatBufferNode source)
        {
            ObjectClass.SetMemberType(fieldIndex, code, asArray);
        }
    }
}
