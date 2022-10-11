namespace FlatCrawler.Lib;

public static class MemoryUtil
{
    public static uint AlignToBytes(uint address, uint alignment)
    {
        return (address + (alignment - 1)) & (~(alignment - 1));
    }

    public static uint AlignForwardAdjustment(uint address, uint alignment)
    {
        uint adjustment = alignment - (address & (alignment - 1));

        if (adjustment == alignment)
            return 0; // already aligned

        return adjustment;
    }

    public static bool IsAligned(uint address, uint alignment)
    {
        return AlignForwardAdjustment(address, alignment) == 0;
    }
}
