namespace FlatCrawler.Lib
{
    public sealed class VTableFieldInfo
    {
        public readonly int Index;
        public readonly int Offset;

        public string? TypeHint { get; set; }

        public VTableFieldInfo(int index, int offset)
        {
            Index = index;
            Offset = offset;
        }

        public override string ToString() => $"[{Offset:X4}]";
    }
}
