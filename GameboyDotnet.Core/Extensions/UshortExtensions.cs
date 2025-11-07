using System.Diagnostics.Contracts;

namespace GameboyDotnet.Extensions;

public static class UshortExtensions
{
    [Pure]
    public static ushort Add(this ushort a, ushort b)
        => (ushort)(a + b);

    [Pure]
    public static ushort Subtract(this ushort a, ushort b)
        => (ushort)(a - b);
    
    [Pure]
    public static ushort GetBit(this ushort value, int bit)
        => (ushort)((value >> bit) & 1);
    
    /// <summary>
    /// Converts ushort to a pair of bytes.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [Pure]
    public static (byte byte1, byte byte2) ToBytesPair(this ushort x)
    {
        return ((byte)(x >> 8), (byte)(x & 0xFF));
    }
}