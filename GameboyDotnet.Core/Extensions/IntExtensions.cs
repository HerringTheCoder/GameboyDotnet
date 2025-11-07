using System.Diagnostics.Contracts;

namespace GameboyDotnet.Extensions;

public static class IntExtensions
{
    /// <summary>
    /// Converts integer to a pair of bytes. Will cause loss of data for values exceeding 16-bits.  
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [Pure]
    public static (byte byte1, byte byte2) ToBytesPair(this int x)
    {
        return ((byte)(x >> 8), (byte)(x & 0xFF));
    }
}