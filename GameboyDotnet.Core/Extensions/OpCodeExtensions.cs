using System.Diagnostics.Contracts;

namespace GameboyDotnet.Extensions;

public static class OpCodeExtensions
{
    /// <summary>
    /// Extracts bits 4 and 5 (LSB), used for extracing R16 register
    /// </summary>
    /// <param name="opCode"></param>
    [Pure]
    public static byte GetR16(this ref byte opCode) => (byte)((opCode & 0b00110000) >> 4);
    
    /// <summary>
    /// Extracts bits 3,4,5 (LSB), used for extracting DestinationR8, OperandR8, BitIndexB3
    /// </summary>
    /// <param name="opCode"></param>
    [Pure]
    public static byte GetDestinationR8(this ref byte opCode) => (byte)((opCode & 0b00111000) >> 3);

    /// <summary>
    /// Extracts first 3 bits (LSB), used for extracting SourceR8 and OperandR8 parts
    /// </summary>
    /// <param name="opCode"></param>
    [Pure]
    public static byte GetSourceR8(this ref byte opCode) => (byte)(opCode & 0b00000111);

    /// <summary>
    /// Extracts bits 3 and 4 (LSB), used for determining conditions
    /// </summary>
    /// <param name="opCode"></param>
    /// <returns></returns>
    [Pure]
    public static byte GetCondition(this ref byte opCode) => (byte)((opCode & 0b00011000) >> 3);
}