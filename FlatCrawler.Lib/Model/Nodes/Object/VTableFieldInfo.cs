namespace FlatCrawler.Lib
{
    public sealed class VTableFieldInfo
    {
        public readonly int Offset;

        public VTableFieldInfo(int offset) => Offset = offset;
        public string? TypeHint { get; set; }

        public override string ToString() => $"[{Offset:X4}]";
    }
}
