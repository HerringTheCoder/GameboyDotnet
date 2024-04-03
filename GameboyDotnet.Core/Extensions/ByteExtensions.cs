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
}