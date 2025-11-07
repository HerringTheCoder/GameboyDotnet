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
    
    [Pure]
    public static int ToInt(this ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > 2)
        {
            throw new ArgumentException("Converting from bytes to int is only supported for a collection of 2 bytes");
        }
        
        return (bytes[0] << 8) | bytes[1];
    }
    
    [Pure]
    public static int ToInt(this Span<byte> bytes)
    {
        if (bytes.Length > 2)
        {
            throw new ArgumentException("Converting from bytes to int is only supported for a collection of 2 bytes");
        }
        
        return (bytes[0] << 8) | bytes[1];
    }
    
    [Pure]
    public static ushort ToUShort(this Span<byte> bytes)
    {
        if (bytes.Length > 2)
        {
            throw new ArgumentException("Converting from bytes to ushort is only supported for a collection of 2 bytes");
        }
        
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }
}