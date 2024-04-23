using GameboyDotnet.Extensions;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Processor;

//Refer to: https://gbdev.io/pandocs/CPU_Instruction_Set.html#block-0 for blocks division
public partial class Cpu
{
    public (byte instructionBytesLength, byte durationTStates) ExecuteBlock0(ref byte opCode)
    {
        return opCode switch
        {
            0x0 => NoOperation(ref opCode),
            0x01 or 0x11 or 0x21 or 0x31 => LoadImmediate16BitIntoR16(ref opCode, opCode.GetR16()),
            0x02 or 0x12 or 0x22 or 0x32 => LoadRegisterAIntoR16Mem(ref opCode, opCode.GetR16()),
            0x0A or 0x1A or 0x2A or 0x3A => LoadR16MemIntoRegisterA(ref opCode, opCode.GetR16()),
            0x08 => LoadSPIntoImmediateMemory(ref opCode),
            0x03 or 0x13 or 0x23 or 0x33 => IncrementR16(ref opCode, opCode.GetR16()),
            0x0B or 0x1B or 0x2B or 0x3B => DecrementR16(ref opCode, opCode.GetR16()),
            0x09 or 0x19 or 0x29 or 0x39 => AddR16ToHL(ref opCode, opCode.GetR16()),
            0x04 or 0x14 or 0x24 or 0x34 or 0x0C or 0x1C or 0x2C or 0x3C
                => IncrementR8(ref opCode, opCode.GetDestinationR8()),
            0x05 or 0x15 or 0x25 or 0x35 or 0x0D or 0x1D or 0x2D or 0x3D
                => DecrementR8(ref opCode, opCode.GetDestinationR8()),
            0x06 or 0x16 or 0x26 or 0x36 or 0x0E or 0x1E or 0x2E or 0x3E
                => LoadImmediate8BitIntoR8(ref opCode, opCode.GetDestinationR8()),
            0x07 => RotateLeftRegisterAThroughCarry(ref opCode),
            0x17 => RotateLeftRegisterA(ref opCode),
            0x0F => RotateRightRegisterAThroughCarry(ref opCode),
            0x1F => RotateRightRegisterA(ref opCode),
            0x27 => DecimalAdjustAccumulator(ref opCode),
            0x2F => ComplementAccumulator(ref opCode),
            0x37 => SetCarryFlag(ref opCode),
            0x3F => ComplementCarryFlag(ref opCode),
            0x18 => JumpRelativeImmediate8bit(ref opCode),
            0x20 or 0x28 or 0x30 or 0x38 => JumpRelativeConditionalImmediate8bit(ref opCode),
            0x10 => Stop(ref opCode),
            _ => NoOperation(ref opCode)
        };
    }

    public (byte instructionBytesLength, byte durationTStates) ExecuteBlock1(ref byte opCode)
    {
        return opCode switch
        {
            0x76 => Halt(),
            _ => LoadSourceR8IntoDestinationR8(ref opCode, opCode.GetDestinationR8(), opCode.GetSourceR8())
        };
    }

    public (byte instructionBytesLength, byte durationTStates) ExecuteBlock2(ref byte opCode)
    {
        return opCode switch
        {
            0x80 or 0x81 or 0x82 or 0x83 or 0x84 or 0x85 or 0x86 or 0x87
                => AddR8ToA(ref opCode, opCode.GetSourceR8()),
            0x88 or 0x89 or 0x8A or 0x8B or 0x8C or 0x8D or 0x8E or 0x8F
                => AddR8ToAWithCarry(ref opCode, opCode.GetSourceR8()),
            0x90 or 0x91 or 0x92 or 0x93 or 0x94 or 0x95 or 0x96 or 0x97
                => SubtractR8FromA(ref opCode, opCode.GetSourceR8()),
            0x98 or 0x99 or 0x9A or 0x9B or 0x9C or 0x9D or 0x9E or 0x9F
                => SubtractR8FromAWithCarry(ref opCode, opCode.GetSourceR8()),
            0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5 or 0xA6 or 0xA7
                => AndR8WithA(ref opCode, opCode.GetSourceR8()),
            0xA8 or 0xA9 or 0xAA or 0xAB or 0xAC or 0xAD or 0xAE or 0xAF
                => XorR8WithA(ref opCode, opCode.GetSourceR8()),
            0xB0 or 0xB1 or 0xB2 or 0xB3 or 0xB4 or 0xB5 or 0xB6 or 0xB7
                => OrR8WithA(ref opCode, opCode.GetSourceR8()),
            0xB8 or 0xB9 or 0xBA or 0xBB or 0xBC or 0xBD or 0xBE or 0xBF
                => CompareR8WithA(ref opCode, opCode.GetSourceR8()),
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
            0xC7 or 0xD7 or 0xE7 or 0xF7 or 0xCF or 0xDF or 0xEF or 0xFF => Restart(ref opCode, n3: opCode.GetDestinationR8()), //n3 covers the same bits as destination r8
            0xCB => ExecuteBlockCB(ref opCode),
            0xC1 or 0xD1 or 0xE1 or 0xF1 => PopR16(ref opCode, opCode.GetR16()),
            0xC5 or 0xD5 or 0xE5 or 0xF5 => PushR16(ref opCode, opCode.GetR16()),
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


    private (byte instructionBytesLength, byte durationTStates) ExecuteBlockCB(ref byte opCode)
    {
        var subOpCode = MemoryController.ReadByte(Register.PC.Add(1));
        if(_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("{opCode:X2} - Executing $CB prefix instruction with subOpCode: {SubOpCode:X2}", opCode, subOpCode);
        var r8 = subOpCode.GetSourceR8();
        var bit3Index = subOpCode.GetDestinationR8();
        var subOpCodeMainIndex = (subOpCode & 0b11000000) >> 6;

        return subOpCodeMainIndex switch
        {
            0b00 =>
                (0xCB00 | subOpCode) switch
                {
                    0xCB00 or 0xCB01 or 0xCB02 or 0xCB03 or 0xCB04 or 0xCB05 or 0xCB06 or 0xCB07
                        => RotateLeftR8ThroughCarry(ref subOpCode, ref r8),
                    0xCB08 or 0xCB09 or 0xCB0A or 0xCB0B or 0xCB0C or 0xCB0D or 0xCB0E or 0xCB0F
                        => RotateRightR8ThroughCarry(ref subOpCode, ref r8),
                    0xCB10 or 0xCB11 or 0xCB12 or 0xCB13 or 0xCB14 or 0xCB15 or 0xCB16 or 0xCB17
                        => RotateLeftR8(ref subOpCode, ref r8),
                    0xCB18 or 0xCB19 or 0xCB1A or 0xCB1B or 0xCB1C or 0xCB1D or 0xCB1E or 0xCB1F
                        => RotateRightR8(ref subOpCode, ref r8),
                    0xCB20 or 0xCB21 or 0xCB22 or 0xCB23 or 0xCB24 or 0xCB25 or 0xCB26 or 0xCB27
                        => ShiftLeftArithmeticallyR8(ref subOpCode, ref r8),
                    0xCB28 or 0xCB29 or 0xCB2A or 0xCB2B or 0xCB2C or 0xCB2D or 0xCB2E or 0xCB2F
                        => ShiftRightArithmeticallyR8(ref subOpCode, ref r8),
                    0xCB30 or 0xCB31 or 0xCB32 or 0xCB33 or 0xCB34 or 0xCB35 or 0xCB36 or 0xCB37
                        => SwapR8Nibbles(ref subOpCode, ref r8),
                    0xCB38 or 0xCB39 or 0xCB3A or 0xCB3B or 0xCB3C or 0xCB3D or 0xCB3E or 0xCB3F
                        => ShiftRightLogicallyR8(ref subOpCode, ref r8),
                    _ => throw new ArgumentOutOfRangeException(nameof(subOpCode))
                },
            0b01 => TestBit3IndexInR8(ref subOpCode, ref bit3Index, ref r8),
            0b10 => ResetBit3IndexInR8(ref subOpCode, ref bit3Index, ref r8),
            0b11 => SetBit3IndexInR8(ref subOpCode, ref bit3Index, ref r8),
            _ => throw new ArgumentOutOfRangeException(nameof(subOpCode))
        };
    }
}