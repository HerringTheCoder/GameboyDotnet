using GameboyDotnet.Extensions;
using Microsoft.Extensions.Logging;

// ReSharper disable InconsistentNaming

namespace GameboyDotnet.Processor;

public partial class Cpu
{
    //nop, 0x00
    private (byte operationBytesLength, byte durationTStates) NoOperation(ref byte opCode)
    {
        return (1, 4);
    }


    /// <summary>
    /// ld r16,imm16 - 0x01, 0x11, 0x21, 0x31
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadImmediate16BitIntoR16(ref byte opCode,
        byte r16)
    {
        _logger.LogDebug("{opCode:X2} - Loading immediate 16 bit value into register, r16 value: {r16} ", opCode, r16);
        var immediate16Bit = MemoryController.ReadWord(Register.PC.Add(1));
        Register.SetRegisterByR16(r16, immediate16Bit);
        return (3, 12);
    }

    /// <summary>
    /// ld [r16mem],a - 0x02, 0x12, 0x22, 0x32
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadRegisterAIntoR16Mem(ref byte opCode, byte r16)
    {
        _logger.LogDebug("{opCode:X2} - Loading register A into memory, r16 value: {r16mem} ", opCode, r16);
        MemoryController.WriteByte(address: Register.GetRegisterValueByR16Mem(r16), Register.A);
        return (1, 8);
    }

    /// <summary>
    /// ld a,[r16mem] - 0x0A, 0x1A, 0x2A, 0x3A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadR16MemIntoRegisterA(ref byte opCode, byte r16)
    {
        _logger.LogDebug("{opCode:X2} - Loading memory into register A, r16mem value: {r16mem} ", opCode, r16);
        Register.A = MemoryController.ReadByte(address: Register.GetRegisterValueByR16Mem(r16));
        return (1, 8);
    }
    
    /// <summary>
    /// ld [imm16],sp - 0x08
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadSPIntoImmediateMemory(ref byte opCode)
    {
        _logger.LogDebug("{opCode:X2} - Loading SP into memory", opCode);
        MemoryController.WriteWord(Register.PC.Add(1), Register.SP);
        return (3, 20);
    }

    /// <summary>
    /// inc r16 - 0x03, 0x13, 0x23, 0x33
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) IncrementR16(ref byte opCode, byte r16)
    {
        _logger.LogDebug("{opCode:X2} - Incrementing 16 bit register, r16 value: {r16} ", opCode, r16);
        Register.SetRegisterByR16(r16, (ushort)(Register.GetRegisterValueByR16(r16) + 1));
        return (1, 8);
    }

    /// <summary>
    /// dec r16 - 0x0B, 0x1B, 0x2B, 0x3B
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) DecrementR16(ref byte opCode, byte r16)
    {
        _logger.LogDebug("{opCode:X2} - Decrementing 16 bit register, r16 value: {r16} ", opCode, r16);
        Register.SetRegisterByR16(r16, (ushort)(Register.GetRegisterValueByR16(r16) - 1));
        return (1, 8);
    }

    /// <summary>
    /// add hl,r16 - 0x09, 0x19, 0x29, 0x39
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) AddR16ToHL(ref byte opCode, byte r16)
    {
        _logger.LogDebug("{opCode:X2} - Adding 16 bit register to HL, r16 value: {r16} ", opCode, r16);
        Set16BitAddCarryFlags(Register.HL, Register.GetRegisterValueByR16(r16));
        Register.HL += Register.GetRegisterValueByR16Mem(r16);
        return (1, 8);
    }

    /// <summary>
    /// inc r8 - 0x04, 0x14, 0x24, 0x34, 0x0C, 0x1C, 0x2C, 0x3C
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) IncrementR8(ref byte opCode, byte r8)
    {
        _logger.LogDebug("{opCode:X2} - Incrementing 8 bit register, r8 value: {r8} ", opCode, r8);
        //6 = [HL], which requires a direct memory read and write
        if (r8 == Constants.R8_HL_Index)
        {
            var memoryAddress = Register.HL;
            var value = MemoryController.ReadByte(memoryAddress);
            Set8BitIncrementCarryFlags(value);
            MemoryController.IncrementByte(memoryAddress);
            return (1, 12);
        }
        else
        {
            var value = Register.GetRegisterValueByR8(r8);
            Set8BitIncrementCarryFlags(value);
            Register.SetRegisterByR8(r8, value.Add(1));
            return (1, 4);
        }
    }

    /// <summary>
    /// dec r8 - 0x05, 0x15, 0x25, 0x35, 0x0D, 0x1D, 0x2D, 0x3D
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) DecrementR8(ref byte opCode, byte r8)
    {
        _logger.LogDebug("{opCode:X2} - Decrementing 8 bit register, r8 value: {r8} ", opCode, r8);
        if (r8 == Constants.R8_HL_Index)
        {
            var memoryAddress = Register.HL;
            Set8BitDecrementCarryFlags(MemoryController.ReadByte(memoryAddress));
            MemoryController.DecrementByte(memoryAddress);
            return (1, 12);
        }

        var value = Register.GetRegisterValueByR8(r8);
        Set8BitDecrementCarryFlags(value);
        Register.SetRegisterByR8(r8, value.Subtract(1));
        return (1, 4);
    }

    /// <summary>
    /// ld r8,imm8 - 0x06, 0x16, 0x26, 0x36, 0x0E, 0x1E, 0x2E, 0x3E
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadImmediate8BitIntoR8(ref byte opCode, byte r8)
    {
        _logger.LogDebug("{opCode:X2} - Loading immediate 8 bit value into register, r8 value: {r8} ", opCode, r8);
        var immediate8Bit = MemoryController.ReadByte(Register.PC.Add(1));
        if (r8 == Constants.R8_HL_Index)
        {
            var memoryAddress = Register.HL;
            MemoryController.WriteByte(memoryAddress, immediate8Bit);
            return (2, 12);
        }

        Register.SetRegisterByR8(r8, immediate8Bit);
        return (2, 8);
    }

    /// <summary>
    /// RLA - 0x17 - Rotate left register A, use old carry bit
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) RotateLeftRegisterA(ref byte opCode)
    {
        _logger.LogDebug("{opCode:X2} - Rotating left register A", opCode);
        var oldCarryFlag = Register.CarryFlag;
        (Register.ZeroFlag, Register.NegativeFlag, Register.HalfCarryFlag) = (false, false, false);
        Register.CarryFlag = (Register.A & 0b1000_0000) != 0; //most significant bit
        Register.A = (byte)(Register.A << 1 | (oldCarryFlag ? 1 : 0));
        return (1, 4);
    }

    /// <summary>
    ///  RRA - 0x1F - Rotate right register A, use old carry bit
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) RotateRightRegisterA(ref byte opCode)
    {
        _logger.LogDebug("{opCode:X2} - Rotating right register A", opCode);
        var oldCarryFlag = Register.CarryFlag;
        (Register.ZeroFlag, Register.NegativeFlag, Register.HalfCarryFlag) = (false, false, false);
        Register.CarryFlag = (Register.A & 0b0000_0001) != 0; //least significant bit
        Register.A = (byte)((Register.A >> 1) | (oldCarryFlag ? 0b1000_0000 : 0));
        return (1, 4);
    }

    /// <summary>
    /// RLCA - 0x0F - Rotate left register A through carry
    /// </summary>
    /// <param name="opCode"></param>
    private (byte instructionBytesLength, byte durationTStates) RotateLeftRegisterAThroughCarry(ref byte opCode)
    {
        _logger.LogDebug("{opCode:X2} - Rotating left register A through carry", opCode);
        (Register.ZeroFlag, Register.NegativeFlag, Register.HalfCarryFlag) = (false, false, false);
        Register.CarryFlag = (Register.A & 0b1000_0000) != 0;
        Register.A = (byte)(Register.A << 1 | (Register.CarryFlag ? 1 : 0));
        return (1, 4);
    }

    /// <summary>
    /// RRCA - 0x0F - Rotate right register A through carry
    /// </summary>
    /// <param name="opCode"></param>
    private (byte instructionBytesLength, byte durationTStates) RotateRightRegisterAThroughCarry(ref byte opCode)
    {
        _logger.LogDebug("{opCode:X2} - Rotating right register A through carry", opCode);
        (Register.ZeroFlag, Register.NegativeFlag, Register.HalfCarryFlag) = (false, false, false);
        Register.CarryFlag = (Register.A & 0b0000_0001) != 0;
        Register.A = (byte)((Register.A >> 1) | (Register.CarryFlag ? 0b1000_0000 : 0));
        return (1, 4);
    }

    /// <summary>
    /// DAA - 0x27 - Decimal adjust accumulator
    /// Credits to Eric Haskins': https://ehaskins.com/2018-01-30%20Z80%20DAA/
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) DecimalAdjustAccumulator(ref byte opCode)
    {
        _logger.LogDebug("{opCode:X2} - Decimal adjust accumulator (DAA)", opCode);

        byte adjust = 0;
        
        if (Register.HalfCarryFlag || (!Register.NegativeFlag && (Register.A & 0x0F) > 9))
            adjust |= 0x06;

        if (Register.CarryFlag || Register is { NegativeFlag: false, A: > 0x99 })
        {
            adjust |= 0x60;
            Register.CarryFlag = true;
        }

        byte result = Register.NegativeFlag ? Register.A.Subtract(adjust) : Register.A.Add(adjust);
        result &= 0xFF;
        Register.A = result;

        Register.HalfCarryFlag = false;
        Register.ZeroFlag = result == 0;
        return (1, 4);
    }

    /// <summary>
    /// cpl - 0x2F - Complement accumulator
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) ComplementAccumulator(ref byte opCode)
    {
        (Register.NegativeFlag, Register.HalfCarryFlag) = (true, true);
        Register.A = (byte)~Register.A;
        return (1, 4);
    }

    /// <summary>
    /// scf - 0x37 - Set carry flag
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) SetCarryFlag(ref byte opCode)
    {
        Register.CarryFlag = true;
        (Register.NegativeFlag, Register.HalfCarryFlag) = (false, false);
        return (1, 4);
    }

    /// <summary>
    /// ccf - 0x3F - Complement carry flag
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) ComplementCarryFlag(ref byte opCode)
    {
        Register.CarryFlag = !Register.CarryFlag;
        (Register.NegativeFlag, Register.HalfCarryFlag) = (false, false);
        return (1, 4);
    }

    /// <summary>
    /// jr e8 (alternative jr imm8) - 0x18 - Jump relative to immediate signed 8 bit
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) JumpRelativeImmediate8bit(ref byte opCode)
    {
        _logger.LogDebug("{opCode:X2} - Jumping relative to immediate signed 8 bit", opCode);
        var immediate8Bit = (sbyte)MemoryController.ReadByte(Register.PC.Add(1));
        Register.PC = (ushort)(Register.PC + immediate8Bit); 
        //TODO: Double check if Register.PC should be incremented by 2 or compensated
        return (2, 12);
    }


    /// <summary>
    /// jr cc, e8 - 0x20, 0x28, 0x30, 0x38 - Jump relative to immediate signed 8 bit with condition
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) JumpRelativeConditionalImmediate8bit(
        ref byte opCode)
    {
        _logger.LogDebug("{opCode:X2} - Jumping relative to immediate signed 8 bit with condition", opCode);
        if (CheckCondition(ref opCode))
            return JumpRelativeImmediate8bit(ref opCode);

        return (2, 8);
    }

    /// <summary>
    /// stop - 0x10 - Stop CPU, also used to switch GBC double speed mode
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) Stop(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X} - STOP - Stopping CPU", opCode);
        IsHalted = true;
        //TODO: Implement GBC mode switch if needed
        return (2, 4);
    }
}