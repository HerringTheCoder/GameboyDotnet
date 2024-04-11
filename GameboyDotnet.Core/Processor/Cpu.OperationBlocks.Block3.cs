using GameboyDotnet.Extensions;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Processor;

public partial class Cpu
{
    /// <summary>
    /// 0xC6 - ADD A, r8 - Add R8 register to A 
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) AddImmediate8BitToA(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - ADD A, r8", opCode);
        var value = MemoryController.ReadByte(Register.PC.Add(1));
        Set8BitAddCarryFlags(Register.A, value);
        Register.A = Register.A.Add(value);
        return (2, 8);
    }

    /// <summary>
    /// 0xCE - ADC A, n8 - Add n8 to A with carry
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) AddImmediate8BitToAWithCarry(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - ADC A, n88", opCode);
        var valueToAdd = MemoryController.ReadByte(Register.PC.Add(1)).Add((byte)(Register.CarryFlag ? 1 : 0));
        Set8BitAddCarryFlags(Register.A, valueToAdd);
        Register.A = Register.A.Add(valueToAdd);
        return (2, 8);
    }

    /// <summary>
    /// 0x6D - SUB A, r8 - Subtract R8 register from A
    /// </summary>
    /// <param name="opCode"></param>
    /// <returns></returns>
    private (byte instructionBytesLength, byte durationTStates) SubtractImmediate8BitFromA(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - SUB A, r8", opCode);
        var value = MemoryController.ReadByte(Register.PC.Add(1));
        Set8BitSubtractCompareFlags(Register.A, value);
        Register.A = Register.A.Subtract(value);
        return (2, 8);
    }

    /// <summary>
    /// 0xDE - SBC A, r8 - Subtract R8 register from A with carry
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) SubtractImmediate8BitFromAWithCarry(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - SBC A, r8", opCode);
        var value = MemoryController.ReadByte(Register.PC.Add(1));
        var valueToSubtract = value.Subtract((byte)(Register.CarryFlag ? 1 : 0));
        Set8BitSubtractCompareFlags(Register.A, value);
        Register.A = Register.A.Subtract(valueToSubtract);
        return (2, 8);
    }

    /// <summary>
    /// 0xE6 - AND A, n8 - Logical AND n8 with A, result in A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) AndImmediate8BitWithA(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - AND A, n8", opCode);
        var value = MemoryController.ReadByte(Register.PC.Add(1));
        Register.A = (byte)(Register.A & value);
        Set8BitAndFlags();
        return (2, 8);
    }

    /// <summary>
    /// 0xEE - XOR A, n8 - Logical XOR n8 with A, result in A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) XorImmediate8BitWithA(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - XOR A, n8", opCode);
        var value = MemoryController.ReadByte(Register.PC.Add(1));
        Register.A = (byte)(Register.A ^ value);
        Set8BitOrXorFlags();
        return (2, 8);
    }

    /// <summary>
    /// 0xF6 - OR A, n8 - Logical OR n8 with A, result in A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) OrImmediate8BitWithA(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - OR A, n8", opCode);
        var value = MemoryController.ReadByte(Register.PC.Add(1));
        Register.A = (byte)(Register.A | value);
        Set8BitOrXorFlags();
        return (2, 8);
    }

    /// <summary>
    /// 0xFE - CP A, n8 - Compare n8 with A
    /// </summary>
    /// <param name="opCode"></param>
    /// <returns></returns>
    private (byte instructionBytesLength, byte durationTStates) CompareImmediate8BitWithA(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - CP A, n8", opCode);
        var value = MemoryController.ReadByte(Register.PC.Add(1));
        Set8BitSubtractCompareFlags(Register.A, value);
        return (2, 8);
    }

    /// <summary>
    /// 0xC0, 0xC8, 0xD0, 0xD8 - RET cc - Return if condition is met
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) ReturnConditional(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - RET cc", opCode);
        if(CheckCondition(ref opCode))
        {
            Register.PC = MemoryController.ReadWord(Register.SP);
            Register.SP = Register.SP.Add(2);
            return (0, 20);
        }

        return (1, 8); 
    }

    /// <summary>
    /// 0xC9 - RET - Return to address at the top of stack
    /// </summary>
    /// <returns></returns>
    private (byte instructionBytesLength, byte durationTStates) Return(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - RET", opCode);
        Register.PC = MemoryController.ReadWord(Register.SP);
        Register.SP = Register.SP.Add(2);
        return (0, 16); //Actual length: 1, but the correct value of PC is already set
    }

    
    /// <summary>
    /// 0xD9 - RETI - Return from interrupt
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private (byte instructionBytesLength, byte durationTStates) ReturnFromInterrupt(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - RETI", opCode);
        Register.PC = MemoryController.ReadWord(Register.SP);
        Register.SP = Register.SP.Add(2);
        Register.InterruptsEnabled = true;
        return (1, 16);
    }

    /// <summary>
    /// 0xC2 or 0xCA or 0xD2 or 0xDA - JP cc, nn - Jump to address if condition is met
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) JumpConditionalImmediate16Bit(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - JP cc, nn", opCode);
        if(CheckCondition(ref opCode))
        {
            Register.PC = MemoryController.ReadWord(Register.PC);
            return (0, 16); //Actual length: 3, but the correct value of PC is already set
        }

        return (3, 12);
    }

    /// <summary>
    /// 0xC3 - JP nn - Jump to address
    /// </summary>
    /// <param name="opCode"></param>
    /// <returns></returns>
    private (byte instructionBytesLength, byte durationTStates) JumpImmediate16Bit(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - JP nn", opCode);
        Register.PC = MemoryController.ReadWord(Register.PC.Add(1));
        return (0, 16); //Actual length: 3, but the correct value of PC is already set
    }

    private (byte instructionBytesLength, byte durationTStates) JumpHL(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - JP HL", opCode);
        Register.PC = Register.HL;
        return (0, 4); //Actual length: 1, but the correct value of PC is already set
    }

    /// <summary>
    /// 0xC4 or 0xCC or 0xD4 or 0xDC - CALL cc, nn - Call subroutine if condition is met
    /// </summary> 
    private (byte instructionBytesLength, byte durationTStates) CallConditionalImmediate16Bit(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - CALL cc, nn", opCode);
        if(CheckCondition(ref opCode))
        {
            Register.SP = Register.SP.Subtract(2);
            MemoryController.WriteWord(Register.SP, Register.PC.Add(3)); //Store the address of the next instruction
            Register.PC = MemoryController.ReadWord(Register.PC.Add(1)); //Jump to the address of imm16
            return (0, 24); //PC already set
        }

        return (3, 12);
    }

    /// <summary>
    /// 0xCD - CALL nn - Call subroutine
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) CallImmediate16Bit(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - CALL nn", opCode);
        Register.SP = Register.SP.Subtract(2);
        MemoryController.WriteWord(Register.SP, Register.PC.Add(3)); //Store the address of the next instruction
        Register.PC = MemoryController.ReadWord(Register.PC.Add(1)); //Jump to the address of imm16
        return (0, 24); //PC already set
    }

    private (byte instructionBytesLength, byte durationTStates) Restart(ref byte opCode, byte n3)
    {
        _logger.LogDebug("{opcode:X2} - RST {n3:X}", opCode, n3);
        Register.SP = Register.SP.Subtract(2);
        MemoryController.WriteWord(Register.SP, Register.PC.Add(1)); //Store the address of the next instruction
        Register.PC = (ushort)(n3 << 3); //<< 3 to multiply by 8
        return (0, 16); //PC already set, thus 0
    }

    /// <summary>
    ///  0xC1 or 0xD1 or 0xE1 or 0xF1  - POP AF - Pop 16-bit value from stack into AF
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) PopR16(ref byte opCode, byte r16stk)
    {
        _logger.LogDebug("{opcode:X2} - POP {getR16:X}", opCode, r16stk);
        var poppedValue = MemoryController.ReadWord(Register.SP);
        
        switch (r16stk)
        {
            case 0:
                Register.BC = poppedValue;
                break;
            case 1:
                Register.DE = poppedValue;
                break;
            case 2:
                Register.HL = poppedValue;
                break;
            case 3:
                Register.A = (byte)((poppedValue & 0xFF00) >> 8);
                SetPopFlags(ref poppedValue);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(r16stk), r16stk, "Invalid r16stk value");
        }
        Register.SP = Register.SP.Add(2);

        return (1, 12);
    }

    /// <summary>
    /// 0xC5 or 0xD5 or 0xE5 or 0xF5 - PUSH AF - Push 16-bit value from AF onto stack
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) PushR16(ref byte opCode, byte r16stk)
    {
        _logger.LogDebug("{opcode:X2} - PUSH {getR16:X}", opCode, r16stk);
        Register.SP = Register.SP.Subtract(2);
        switch(r16stk) 
        {
            case 0:
                MemoryController.WriteWord(Register.SP, Register.BC);
                break;
            case 1:
                MemoryController.WriteWord(Register.SP, Register.DE);
                break;
            case 2:
                MemoryController.WriteWord(Register.SP, Register.HL);
                break;
            case 3:
                MemoryController.WriteByte(Register.SP.Add(1), Register.A);
                MemoryController.WriteByte(Register.SP, (byte)(Register.F & 0xF0)); //Push only the upper 4 bits of F
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(r16stk), r16stk, "Invalid r16stk value");
        }
        return (1, 16);
    }

    /// <summary>
    /// 0xE2 - LDH (C), A - Load A into (FF00 + C) address
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadAIntoFF00PlusCAddress(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - LDH (C), A", opCode);
        MemoryController.WriteByte((ushort)(0xFF00 + Register.C), Register.A);
        return (1, 8);
    }

    /// <summary>
    /// 0xE0 - LDH (n), A - Load A into (FF00 + immediate 8-bit) address
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadAIntoImmediate8BitAddress(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - LDH (n), A", opCode);
        var address = MemoryController.ReadByte(Register.PC.Add(1));
        MemoryController.WriteByte((ushort)(0xFF00 + address), Register.A);
        return (2, 12);
    }

    /// <summary>
    /// 0xEA - LD (nn), A - Load A into immediate 16-bit address
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadAIntoImmediate16BitAddress(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - LD (nn), A", opCode);
        var address = MemoryController.ReadWord(Register.PC.Add(1));
        MemoryController.WriteByte(address, Register.A);
        return (3, 16);
    }

    /// <summary>
    /// 0xF2 - LDH A, (C) - Load (FF00 + C) address value into A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadFF00PlusCAddressValueIntoA(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - LDH A, (C)", opCode);
        Register.A = MemoryController.ReadByte((ushort)(0xFF00 + Register.C));
        return (1, 8);
    }

    /// <summary>
    /// 0xF0 - LDH A, (n) - Load (FF00 + immediate 8-bit) address value into A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadImmediate8BitAddressValueIntoA(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - LDH A, (n)", opCode);
        var address = MemoryController.ReadByte(Register.PC.Add(1));
        Register.A = MemoryController.ReadByte((ushort)(0xFF00 + address));
        return (2, 12);
    }

    /// <summary>
    /// 0xFA - LDH A, (nn) - Load (FF00 + immediate 16-bit) address value into A
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadImmediate16BitAddressValueIntoA(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - LDH A, (nn)", opCode);
        var address = MemoryController.ReadWord(Register.PC.Add(1));
        Register.A = MemoryController.ReadByte(address);
        return (3, 16);
    }

    /// <summary>
    /// 0xE8 - ADD SP, n - Add immediate 8-bit to SP
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) AddImmediate8BitToSP(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - ADD SP, n", opCode);
        var signedImmediate8Bit = (sbyte)MemoryController.ReadByte(Register.PC.Add(1));
        SetSPSignedByteAddFlags((byte)(Register.SP & 0x00FF), signedImmediate8Bit);
        Register.SP = (ushort)(Register.SP + signedImmediate8Bit);
        return (2, 16);
    }

    /// <summary>
    /// 0xF8 - LD HL, SP + n - Load SP + immediate 8-bit into HL
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadSPPlusImmediate8BitIntoHL(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - LD HL, SP + n", opCode);
        var signedImmediate8Bit = (sbyte)MemoryController.ReadByte(Register.PC.Add(1));
        SetSPSignedByteAddFlags((byte)(Register.SP & 0x00FF), signedImmediate8Bit);
        Register.HL = (ushort)(Register.SP + signedImmediate8Bit);
        return (2, 12);
    }

    /// <summary>
    /// 0xF9 - LD SP, HL - Load HL into SP
    /// </summary>
    /// <param name="opCode"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private (byte instructionBytesLength, byte durationTStates) LoadHLIntoSP(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - LD SP, HL", opCode);
        Register.SP = Register.HL;
        return (1, 8);
    }

    /// <summary>
    /// 0xF3 - DI - Disable interrupts
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) DisableInterrupts(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - DI", opCode);
        Register.InterruptsEnabled = false;
        return (1, 4);
    }

    /// <summary>
    /// 0xFB - EI - Enable interrupts
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) EnableInterrupts(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - EI", opCode);
        Register.InterruptsEnabled = true;
        return (1, 4);
    }
}