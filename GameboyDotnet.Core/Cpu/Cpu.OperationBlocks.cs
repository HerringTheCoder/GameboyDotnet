namespace GameboyDotnet.Components.Cpu;

//Refer to: https://gbdev.io/pandocs/CPU_Instruction_Set.html#block-0 for blocks division
public partial class Cpu
{
    public (byte instructionBytesLength, byte durationTStates) ExecuteBlock0(ref byte opCode)
    {
        return opCode switch
        {
            0x0 => NoOperation(ref opCode),
            0x01 or 0x11 or 0x21 or 0x31 => LoadImmediate16BitIntoR16(ref opCode, GetR16(ref opCode)),
            0x02 or 0x12 or 0x22 or 0x32 => LoadRegisterAIntoR16Mem(ref opCode, GetR16(ref opCode)),
            0x0A or 0x1A or 0x2A or 0x3A => LoadR16MemIntoRegisterA(ref opCode, GetR16(ref opCode)),
            0x08 => LoadSPIntoImmediateMemory(ref opCode),
            0x03 or 0x13 or 0x23 or 0x33 => IncrementR16(ref opCode, GetR16(ref opCode)),
            0x0B or 0x1B or 0x2B or 0x3B => DecrementR16(ref opCode, GetR16(ref opCode)),
            0x09 or 0x19 or 0x29 or 0x39 => AddR16ToHL(ref opCode, GetR16(ref opCode)),
            0x04 or 0x14 or 0x24 or 0x34 or 0x0C or 0x1C or 0x2C or 0x3C 
                => IncrementR8(ref opCode, GetDestinationR8(ref opCode)),
            0x05 or 0x15 or 0x25 or 0x35 or 0x0D or 0x1D or 0x2D or 0x3D 
                => DecrementR8(ref opCode, GetDestinationR8(ref opCode)),
            0x06 or 0x16 or 0x26 or 0x36 or 0x0E or 0x1E or 0x2E or 0x3E 
                => LoadImmediate8BitIntoR8(ref opCode, GetDestinationR8(ref opCode)),
            0x07 => RotateLeftRegisterA(ref opCode),
            0x0F => RotateRightRegisterA(ref opCode),
            0x17 => RotateLeftRegisterAThroughCarry(ref opCode),
            0x1F => RotateRightRegisterAThroughCarry(ref opCode),
            0x27 => DecimalAdjustAccumulator(ref opCode),
            0x2F => ComplementAccumulator(ref opCode),
            0x37 => SetCarryFlag(ref opCode),
            0x3F => ComplementCarryFlag(ref opCode),
            0x18 => JumpRelativeImmediate8bit(ref opCode),
            0x20 or 0x28 or 0x30 or 0x38 => JumpRelativeConditionalImmediate8bit(ref opCode),
            0b000_1_0000 => Stop(),
            _ => NoOperation(ref opCode)
        };
    }

    public (byte instructionBytesLength, byte durationTStates) ExecuteBlock1(ref byte opCode)
    {
        return opCode switch
        {
            0b01110110 => Halt(),
            _ => LoadSourceR8IntoDestinationR8(ref opCode, GetDestinationR8(ref opCode), GetSourceR8(ref opCode))
        };
    }

    public (byte instructionBytesLength, byte durationTStates) ExecuteBlock2(ref byte opCode)
    {
        return opCode switch
        {
            0x80 or 0x81 or 0x82 or 0x83 or 0x84 or 0x85 or 0x86 or 0x87 
                => AddR8ToA(ref opCode, GetSourceR8(ref opCode)),
            0x88 or 0x89 or 0x8A or 0x8B or 0x8C or 0x8D or 0x8E or 0x8F 
                => AddR8ToAWithCarry(ref opCode, GetSourceR8(ref opCode)),
            0x90 or 0x91 or 0x92 or 0x93 or 0x94 or 0x95 or 0x96 or 0x97 
                => SubtractR8FromA(ref opCode, GetSourceR8(ref opCode)),
            0x98 or 0x99 or 0x9A or 0x9B or 0x9C or 0x9D or 0x9E or 0x9F 
                => SubtractR8FromAWithCarry(ref opCode, GetSourceR8(ref opCode)),
            0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5 or 0xA6 or 0xA7 
                => AndR8WithA(ref opCode, GetSourceR8(ref opCode)),
            0xA8 or 0xA9 or 0xAA or 0xAB or 0xAC or 0xAD or 0xAE or 0xAF
                => XorR8WithA(ref opCode, GetSourceR8(ref opCode)),
            0xB0 or 0xB1 or 0xB2 or 0xB3 or 0xB4 or 0xB5 or 0xB6 or 0xB7 
                => OrR8WithA(ref opCode, GetSourceR8(ref opCode)),
            0xB8 or 0xB9 or 0xBA or 0xBB or 0xBC or 0xBD or 0xBE or 0xBF 
                => CompareR8WithA(ref opCode, GetSourceR8(ref opCode)),
            _ => throw new ArgumentOutOfRangeException(nameof(opCode), opCode, null)
        };
    }

    public (byte instructionBytesLength, byte durationTStates) ExecuteBlock3(ref byte opCode)
    {
        return opCode switch
        {
            0xC6 => AddImmediate8BitToA(ref opCode),
            0xCE => AddImmediate8BitToAWithCarry(ref opCode),
            0xD6 => SubtractImmediate8BitFromA(ref opCode),
            0xDE => SubtractImmediate8BitFromAWithCarry(ref opCode),
            0xE6 => AndImmediate8BitWithA(ref opCode),
            0xEE => XorImmediate8BitWithA(ref opCode),
            0xF6 => OrImmediate8BitWithA(ref opCode),
            0xFE => CompareImmediate8BitWithA(ref opCode),
            0xC0 or 0xC8 or 0xD0 or 0xD8 => ReturnConditional(ref opCode),
            0xC9 => Return(ref opCode),
            0xD9 => ReturnFromInterrupt(ref opCode),
            0xC2 or 0xCA or 0xD2 or 0xDA => JumpConditionalImmediate16Bit(ref opCode),
            0xC3 => JumpImmediate16Bit(ref opCode),
            0xE9 => JumpHL(ref opCode),
            0xC4 or 0xCC or 0xD4 or 0xDC => CallConditionalImmediate16Bit(ref opCode),
            0xCD => CallImmediate16Bit(ref opCode),
            0xCF => Restart(ref opCode, n3: GetDestinationR8(ref opCode)), //n3 covers the same bits as destination r8
            0xCB => ExecuteBlockCB(ref opCode),
            0xC1 or 0xC9 or 0xD1 or 0xD9 => PopR16(ref opCode, GetR16(ref opCode)),
            0xC5 or 0xD5 or 0xE5 or 0xF5 => PushR16(ref opCode, GetR16(ref opCode)),
            0xE2 => LoadAIntoFF00PlusCAddress(ref opCode),
            0xE0 => LoadAIntoImmediate8BitAddress(ref opCode),
            0xEA => LoadAIntoImmediate16BitAddress(ref opCode),
            0xF2 => LoadFF00PlusCAddressValueIntoA(ref opCode),
            0xF0 => LoadImmediate8BitAddressValueIntoA(ref opCode),
            0xFA => LoadImmediate16BitAddressValueIntoA(ref opCode),
            0xE8 => AddImmediate8BitToSP(ref opCode),
            0xF8 => LoadSPPlusImmediate8BitIntoHL(ref opCode),
            0xF9 => LoadHLIntoSP(ref opCode),
            0xF3 => DisableInterrupts(ref opCode),
            0xFB => EnableInterrupts(ref opCode),
            0xDB or 0xDD or 0xE3 or 0xE4 or 0xEB or 0xEC or 0xED or 0xF4 or 0xFC or 0xFD 
                => throw new Exception("CPU Hard Lock!"),
            _ => throw new ArgumentOutOfRangeException(nameof(opCode), opCode, null)
        };
    }
}