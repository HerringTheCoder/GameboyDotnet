using System.Diagnostics.Contracts;

namespace GameboyDotnet.Extensions;

public static class ByteExtensions
{
    [Pure]
    public static byte Add(this byte a, byte b)
        => (byte)(a + b);

    [Pure]
    public static byte Subtract(this byte a, byte b)
        => (byte)(a - b);
    
    [Pure]
    public static byte SetBit(this byte b, int bitIndex)
        => (byte)(b | (1 << bitIndex));
    
    [Pure]
    public static byte ClearBit(this byte b, int bitIndex)
        => (byte)(b & ~(1 << bitIndex));
    
    
    [Pure]
    public static bool IsBitSet(this byte b, int bitIndex)
        => (b & (1 << bitIndex)) != 0;
}