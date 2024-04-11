using GameboyDotnet.Extensions;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Processor;

public partial class Cpu
{
    /// <summary>
    /// ADD A, r8 - Add R8 register to A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) AddR8ToA(ref byte opCode, byte r8)
    {
        _logger.LogDebug("{opcode:X2} - ADD A, {getSourceR8:X}", opCode, r8);
        var value = r8 == Constants.R8_HL_Index
            ? MemoryController.ReadByte(Register.HL)
            : Register.GetRegisterValueByR8(r8);

        Set8BitAddCarryFlags(Register.A, value);
        Register.A = Register.A.Add(value);
        return (1, (byte)(r8 == 0b110 ? 8 : 4));
    }

    /// <summary>
    /// ADC A, r8 - Add R8 register to A with carry
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) AddR8ToAWithCarry(ref byte opCode, byte r8)
    {
        _logger.LogDebug("{opcode:X2} - ADC A, {getSourceR8:X}", opCode, r8);
        var value = r8 == Constants.R8_HL_Index
            ? MemoryController.ReadByte(Register.HL)
            : Register.GetRegisterValueByR8(r8);

        var valueToAdd = MemoryController.ReadByte(Register.PC.Add(1)).Add((byte)(Register.CarryFlag ? 1 : 0));
        Set8BitAddCarryFlags(Register.A, valueToAdd);
        Register.A = Register.A.Add(valueToAdd);
        return (1, (byte)(r8 == 0b110 ? 8 : 4));
    }

    /// <summary>
    /// SUB A, r8 - Subtract R8 register from A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) SubtractR8FromA(ref byte opCode, byte r8)
    {
        _logger.LogDebug("{opcode:X2} - SUB A, {getSourceR8:X}", opCode, r8);
        var value = r8 == Constants.R8_HL_Index
            ? MemoryController.ReadByte(Register.HL)
            : Register.GetRegisterValueByR8(r8);

        Set8BitSubtractCompareFlags(Register.A, value);
        Register.A = Register.A.Subtract(value);
        return (1, (byte)(r8 == 0b110 ? 8 : 4));
    }

    /// <summary>
    /// SBC A, r8 - Subtract R8 register from A with carry
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) SubtractR8FromAWithCarry(ref byte opCode, byte r8)
    {
        _logger.LogDebug("{opcode:X2} - SBC A, {getSourceR8:X}", opCode, r8);
        var value = r8 == 0b110
            ? MemoryController.ReadByte(Register.HL)
            : Register.GetRegisterValueByR8(r8);
        
        var valueToSubtract = value.Subtract((byte)(Register.CarryFlag ? 1 : 0));
        Set8BitSubtractCompareFlags(Register.A, value);
        Register.A = Register.A.Subtract(valueToSubtract);
        return (1, (byte)(r8 == 0b110 ? 8 : 4));
    }

    /// <summary>
    /// AND A, r8 - Logical AND R8 register with A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) AndR8WithA(ref byte opCode, byte r8)
    {
        _logger.LogDebug("{opcode:X2} - AND A, {r8R8:X}", opCode, r8);
        var value = r8 == 0b110
            ? MemoryController.ReadByte(Register.HL)
            : Register.GetRegisterValueByR8(r8);

        Register.A = (byte)(Register.A & value);
        Set8BitAndFlags();
        return (1, (byte)(r8 == 0b110 ? 8 : 4));
    }

    /// <summary>
    /// XOR A, r8 - Logical XOR R8 register with A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) XorR8WithA(ref byte opCode, byte r8)
    {
        _logger.LogDebug("{opcode:X2} - XOR A, {r8R8:X}", opCode, r8);
        var value = r8 == 0b110
            ? MemoryController.ReadByte(Register.HL)
            : Register.GetRegisterValueByR8(r8);

        Register.A = (byte)(Register.A ^ value);
        Set8BitOrXorFlags();
        return (1, (byte)(r8 == 0b110 ? 8 : 4));
    }

    /// <summary>
    /// OR A, r8 - Logical OR R8 register with A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) OrR8WithA(ref byte opCode, byte r8)
    {
        _logger.LogDebug("{opcode:X2} - OR A, {r8R8:X}", opCode, r8);
        var value = r8 == 0b110
            ? MemoryController.ReadByte(Register.HL)
            : Register.GetRegisterValueByR8(r8);

        Register.A = (byte)(Register.A | value);
        Set8BitOrXorFlags();
        return (1, (byte)(r8 == 0b110 ? 8 : 4));
    }

    private (byte instructionBytesLength, byte durationTStates) CompareR8WithA(ref byte opCode, byte r8)
    {
        _logger.LogDebug("{opcode:X2} - CP A, {r8R8:X}", opCode, r8);
        var value = r8 == 0b110
            ? MemoryController.ReadByte(Register.HL)
            : Register.GetRegisterValueByR8(r8);

        //CP is effectively the same as SUB, but without storing the result, so we can reuse the SUB flags method
        Set8BitSubtractCompareFlags(Register.A, value);
        return (1, (byte)(r8 == 0b110 ? 8 : 4));
    }
}