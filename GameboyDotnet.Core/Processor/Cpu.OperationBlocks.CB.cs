using GameboyDotnet.Extensions;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Processor;

public partial class Cpu
{
    /// <summary>
    /// RL r8 - Rotate left r8, set lsb to old carry flag, shift left, set carry to previous msb
    /// </summary>
    /// <param name="opCode"></param>
    /// <param name="r8"></param>
    private (byte instructionBytesLength, byte durationTStates) RotateLeftR8(ref byte opCode, ref byte r8)
    {
        _logger.LogDebug("CB{OpCode:X2} - RL r8", opCode);

        byte RotateLeftCb(ref byte registerValue) //TODO: Extract this and other local functions
        {
            var oldCarryFlag = Register.CarryFlag;
            (Register.NegativeFlag, Register.HalfCarryFlag) = (false, false);
            Register.CarryFlag = (registerValue & 0b1000_0000) != 0;
            var result = (byte)(registerValue << 1 | (oldCarryFlag ? 1 : 0));
            Register.ZeroFlag = result == 0;
            return result;
        }
        
        if (r8 == Constants.R8_HL_Index)
        {
            var memValue = MemoryController.ReadByte(Register.HL);
            MemoryController.WriteByte(Register.HL, RotateLeftCb(ref memValue));
            return (2, 16);
        }
        
        var value = Register.GetRegisterValueByR8(r8);
        Register.SetRegisterByR8(r8, RotateLeftCb(ref value));
        return (2, 8);
    }

    /// <summary>
    /// RR r8 - Rotate right r8, set msb to old carry flag, shift right, set carry to previous lsb
    /// </summary>
    /// <param name="opCode"></param>
    /// <param name="r8"></param>
    public (byte instructionLength, byte Tstates) RotateRightR8(ref byte opCode, ref byte r8)
    {
        _logger.LogDebug("CB{OpCode:X2} - RR r8", opCode);
        
        byte RotateRightCb(ref byte registerValue)
        {
            var oldCarryFlag = Register.CarryFlag;
            (Register.ZeroFlag, Register.NegativeFlag, Register.HalfCarryFlag) = (false, false, false);
            Register.CarryFlag = (registerValue & 0b0000_0001) != 0; //least significant bit
            var value = (byte)((registerValue >> 1) | (oldCarryFlag ? 0b1000_0000 : 0));
            Register.ZeroFlag = value == 0;
            return value;
        }

        if (r8 == Constants.R8_HL_Index)
        {
            var memValue = MemoryController.ReadByte(Register.HL);
            MemoryController.WriteByte(Register.HL, RotateRightCb(ref memValue));
            return (2, 16);
        }
        
        var value = Register.GetRegisterValueByR8(r8);
        Register.SetRegisterByR8(r8, RotateRightCb(ref value));
        return (2, 8);
    }
    
    /// <summary>
    /// RLC r8 - Rotate left r8, set carry to msb, then set lsb to msb (bit 7 -> bit 0)
    /// </summary>
    /// <param name="opCode"></param>
    /// <param name="r8"></param>
    private (byte instructionBytesLength, byte durationTStates) RotateLeftR8ThroughCarry(ref byte opCode, ref byte r8)
    {
        _logger.LogDebug("CB{OpCode:X2} - RLC r8", opCode);

        byte RotateLeftThroughCarryCb(ref byte registerValue) //TODO: Extract this and RLC, RR, RRC
        {
            (Register.NegativeFlag, Register.HalfCarryFlag) = (false, false);
            Register.CarryFlag = (registerValue & 0b1000_0000) != 0;
            var result = (byte)(registerValue << 1 | (Register.CarryFlag ? 1 : 0));
            Register.ZeroFlag = result == 0;
            return result;
        }
        
        if (r8 == Constants.R8_HL_Index)
        {
            var memValue = MemoryController.ReadByte(Register.HL);
            MemoryController.WriteByte(Register.HL, RotateLeftThroughCarryCb(ref memValue));
            return (2, 16);
        }
        
        var value = Register.GetRegisterValueByR8(r8);
        Register.SetRegisterByR8(r8, RotateLeftThroughCarryCb(ref value));
        return (2, 8);
    }

    /// <summary>
    /// RRC r8 - Rotate right r8, set carry to lsb, then set msb to lsb (bit 0 -> bit 7)
    /// </summary>
    /// <param name="opCode"></param>
    /// <param name="r8"></param>
    public (byte instructionLength, byte Tstates) RotateRightR8ThroughCarry(ref byte opCode, ref byte r8)
    {
        _logger.LogDebug("CB{OpCode:X2} - RRC r8", opCode);
        
        byte RotateRightThroughCarryCb(ref byte registerValue)
        {
            (Register.NegativeFlag, Register.HalfCarryFlag) = (false, false);
            Register.CarryFlag = (registerValue & 0b0000_0001) != 0;
            var value = (byte)((registerValue >> 1) | (Register.CarryFlag ? 0b1000_0000 : 0));
            Register.ZeroFlag = value == 0;
            return value;
        }

        if (r8 == Constants.R8_HL_Index)
        {
            var memValue = MemoryController.ReadByte(Register.HL);
            MemoryController.WriteByte(Register.HL, RotateRightThroughCarryCb(ref memValue));
            return (2, 16);
        }
        
        var value = Register.GetRegisterValueByR8(r8);
        Register.SetRegisterByR8(r8, RotateRightThroughCarryCb(ref value));
        return (2, 8);
    }

    /// <summary>
    /// SLA r8 - Shift Left Arithmetically
    /// </summary>
    /// <param name="opCode"></param>
    /// <param name="r8"></param>
    /// <returns></returns>
    private (byte instructionBytesLength, byte durationTStates) ShiftLeftArithmeticallyR8(ref byte opCode, ref byte r8)
    {
        _logger.LogDebug("CB{OpCode:X2} - SLA r8", opCode);

        byte ShiftLeft(ref byte registerValue)
        {
            Register.CarryFlag = (registerValue & 0b1000_0000) != 0;
            var value = (byte)(registerValue << 1);
            Register.ZeroFlag = value == 0;
            return value;
        }

        if (r8 == Constants.R8_HL_Index)
        {
            var memValue = MemoryController.ReadByte(Register.HL);
            MemoryController.WriteByte(Register.HL, ShiftLeft(ref memValue));
            return (2, 16);
        }

        var value = Register.GetRegisterValueByR8(r8);
        Register.SetRegisterByR8(r8, ShiftLeft(ref value));
        return (2, 8);
    }

    /// <summary>
    /// SRA r8 - Shift Right Arithematiclly
    /// Important! Bit 7 should stay the same after shift, i.e. 0b1000_0010 will become 0b1100_0001
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) ShiftRightArithmeticallyR8(ref byte opCode, ref byte r8)
    {
        _logger.LogDebug("CB{OpCode:X2} - SLA r8", opCode);

        byte ShiftLeft(ref byte registerValue)
        {
            Register.CarryFlag = (registerValue & 0b0000_0001) != 0;
            var value = (byte)(registerValue >> 1);
            Register.ZeroFlag = value == 0;
            return value;
        }

        if (r8 == Constants.R8_HL_Index)
        {
            var memValue = MemoryController.ReadByte(Register.HL);
            MemoryController.WriteByte(Register.HL, ShiftLeft(ref memValue));
            return (2, 16);
        }

        var value = Register.GetRegisterValueByR8(r8);
        Register.SetRegisterByR8(r8, ShiftLeft(ref value));
        return (2, 8);
    }

    /// <summary>
    /// SWAP r8 - Swaps upper and lower nibbles in r8
    /// </summary>
    /// <param name="subOpCode"></param>
    /// <param name="r8"></param>
    /// <returns></returns>
    private (byte instructionBytesLength, byte durationTStates) SwapR8Nibbles(ref byte subOpCode, ref byte r8)
    {
        _logger.LogDebug("CB{OpCode:X2} - SWAP r8", subOpCode);

        byte SwapNibbles(ref byte registerValue)
        {
            (Register.CarryFlag, Register.HalfCarryFlag, Register.NegativeFlag) = (false, false, false);
            var value = (byte)(((registerValue & 0b00001111) << 4) | ((registerValue & 0b11110000) >> 4));
            Register.ZeroFlag = value == 0;
            return value;
        }
        
        if (r8 == Constants.R8_HL_Index)
        {
            var memValue = MemoryController.ReadByte(Register.HL);
            MemoryController.WriteByte(Register.HL, SwapNibbles(ref memValue));
            return (2, 16);
        }

        var value = Register.GetRegisterValueByR8(r8);
        Register.SetRegisterByR8(r8, SwapNibbles(ref value));
        return (2, 8);
    }

    /// <summary>
    /// SRL r8 - Shift Right Logically
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) ShiftRightLogicallyR8(ref byte subOpCode, ref byte r8)
    {
        _logger.LogDebug("CB{OpCode:X2} - SRL r8", subOpCode);

        byte ShiftRightLogically(ref byte registerValue)
        {
            Register.CarryFlag = (registerValue & 0b0000_0001) != 0;
            var result = (byte)(registerValue >> 1);
            Register.ZeroFlag = result == 0;
            return result;
        }

        if (r8 == Constants.R8_HL_Index)
        {
            var memValue = MemoryController.ReadByte(Register.HL);
            MemoryController.WriteByte(Register.HL, ShiftRightLogically(ref memValue));
            return (2, 16);
        }

        var value = Register.GetRegisterValueByR8(r8);
        Register.SetRegisterByR8(r8, ShiftRightLogically(ref value));
        return (2, 8);
    }

    /// <summary>
    /// BIT u3 r8 - Test bit u3 in r8 by setting Zero flag accordingly
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) TestBit3IndexInR8(ref byte subOpCode, ref byte bit3Index, ref byte r8)
    {
        _logger.LogDebug("CB{SubOpCode:X2} - BIT u3 r8", subOpCode);
        (Register.NegativeFlag, Register.HalfCarryFlag) = (false, false);
        var mask = 0b0000_0001 << bit3Index; //Set tested bit
        if (r8 == Constants.R8_HL_Index)
        {
            var memValue = MemoryController.ReadByte(Register.HL);
            Register.ZeroFlag = (memValue & mask) == 0;
            return (2, 12);
        }

        var value = Register.GetRegisterValueByR8(r8);
        Register.ZeroFlag = (value & mask) == 0;
        return (2, 8);
    }

    /// <summary>
    /// RES u3 r8 - Reset (set to zero) bit u3 in r8 
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) ResetBit3IndexInR8(ref byte subOpCode, ref byte bit3Index, ref byte r8)
    {
        _logger.LogDebug("CB{SubOpCode:X2} - RES u3 r8", subOpCode);
        var mask = ~(1 << bit3Index);
        if (r8 == Constants.R8_HL_Index)
        {
            var memValue = MemoryController.ReadByte(Register.HL);
            MemoryController.WriteByte(Register.HL, (byte)(memValue & mask));
            return (2, 16);
        }

        var value = Register.GetRegisterValueByR8(r8);
        Register.SetRegisterByR8(r8, (byte)(value & mask));
        return (2, 8);
    }
    
    /// <summary>
    /// SET u3 r8 - SET (set to 1) bit u3 in r8 
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) SetBit3IndexInR8(ref byte subOpCode, ref byte bit3Index, ref byte r8)
    {
        _logger.LogDebug("CB{SubOpCode:X2} - RES u3 r8", subOpCode);
        var mask = 1 << bit3Index;
        if (r8 == Constants.R8_HL_Index)
        {
            var memValue = MemoryController.ReadByte(Register.HL);
            MemoryController.WriteByte(Register.HL, (byte)(memValue | mask));
        }

        var value = Register.GetRegisterValueByR8(r8);
        Register.SetRegisterByR8(r8, (byte)(value | mask));
        return (2, 8);
    }
}