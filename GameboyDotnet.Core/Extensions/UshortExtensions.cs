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
}