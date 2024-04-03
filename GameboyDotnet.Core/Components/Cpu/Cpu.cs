using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Components.Cpu;

public partial class Cpu
{
    internal CpuRegister Register { get; set; } = new();
    internal MemoryController MemoryController { get; }
    public bool IsHalted { get; set; }

    private readonly ILogger<Gameboy> _logger;

    public Cpu(ILogger<Gameboy> logger)
    {
        _logger = logger;
        MemoryController = new MemoryController(logger);
    }

    public void ExecuteNextOperation()
    {
        var opCode = MemoryController.ReadByte(Register.PC);
        
        //Idea of operation blocks is based on: https://gbdev.io/pandocs/CPU_Instruction_Set.html
        var operationBlock = opCode & 0b11000000;

        var operationSize = operationBlock switch
        {
            0b00 => ExecuteBlock0(ref opCode),
            0b01 => ExecuteBlock1(ref opCode),
            0b10 => ExecuteBlock2(ref opCode),
            0b11 => ExecuteBlock3(ref opCode),
            _ => throw new NotImplementedException($"Operation {opCode:X} not implemented")
        };

        Register.PC += operationSize.instructionBytesLength;
    }

    private byte GetR16(ref byte opCode) => (byte)((opCode & 0b00110000) >> 4);
    
    private byte GetDestinationR8(ref byte opCode) => (byte)(opCode & 0b00111000 >> 3);

    private byte GetSourceR8(ref byte opCode) => (byte)(opCode & 0b00000111);

    private byte GetCondition(ref byte opCode) => (byte)(opCode & 0b00011000 >> 3);

    private bool CheckCondition(ref byte opCode)
    {
        _logger.LogDebug("{opcode:X2} - Checking condition flag", opCode);
        return GetCondition(ref opCode) switch
        {
            0b00 => !Register.ZeroFlag,
            0b01 => Register.ZeroFlag,
            0b10 => !Register.CarryFlag,
            0b11 => Register.CarryFlag,
            _ => throw new ArgumentOutOfRangeException()
        };
    }


    private void Set16BitAddCarryFlags(ushort a, ushort b)
    {
        Register.NegativeFlag = false;
        Register.HalfCarry = (a & 0xFFF) + (b & 0xFFF) > 0xFFF; //Check overflow from 11 bit to 12 bit
        Register.CarryFlag = a + b > 0xFFFF;
    }

    private void Set8BitAddCarryFlags(byte a, byte b)
    {
        Register.NegativeFlag = false;
        Register.ZeroFlag = (byte)(a + b) == 0;
        Register.HalfCarry = (a & 0xF) + (b & 0xF) > 0xF; //Check overflow from 3 bit to 4 bit
        Register.CarryFlag = a + b > 0xFF;
    }

    private void Set8BitIncrementCarryFlags(byte a)
    {
        Register.NegativeFlag = false;
        Register.ZeroFlag = (byte)(a + 1) == 0;
        Register.HalfCarry = (a & 0xF) + 1 > 0xF; //Check overflow from 3 bit to 4 bit
    }

    private void Set8BitDecrementCarryFlags(byte value)
    {
        Register.NegativeFlag = true;
        Register.ZeroFlag = (byte)(value - 1) == 0;
        //Check underflow from bit 4 by isolating the 4th bit and checking if it's 0 (will underflow) or 1 (won't underflow)
        Register.HalfCarry = (value & 0xF) == 0;
    }

    private void Set8BitSubtractCompareFlags(byte a, byte b)
    {
        Register.NegativeFlag = true;
        Register.ZeroFlag = (byte)(a - b) == 0;
        Register.HalfCarry = (a & 0xF) < (b & 0xF); //Check underflow from 4 bit to 3 bit
        Register.CarryFlag = a < b;
    }
    
    private void Set8BitAndFlags()
    {
        Register.ZeroFlag = Register.A == 0;
        Register.HalfCarry = true;
        (Register.NegativeFlag, Register.CarryFlag) = (false, false);
    }
    
    private void Set8BitOrXorFlags()
    {
        Register.ZeroFlag = Register.A == 0;
        (Register.NegativeFlag, Register.HalfCarry, Register.CarryFlag) = (false, false, false);
    }

    private void SetPopFlags(ref ushort poppedValue)
    {
        var lowByte = (byte)(poppedValue & 0xFF00);
        Register.ZeroFlag = (lowByte & 0b10000000) != 0;
        Register.NegativeFlag = (lowByte & 0b01000000) != 0;
        Register.HalfCarry = (lowByte & 0b00100000) != 0;
        Register.CarryFlag = (lowByte & 0b00010000) != 0;
    }
    
    private void SetSPSignedByteAddFlags(ushort sp, sbyte signedByte)
    {
        Register.ZeroFlag = false;
        Register.NegativeFlag = false;
        Register.HalfCarry = (sp & 0xF) + (signedByte & 0xF) > 0xF;
        Register.CarryFlag = (sp & 0xFF) + signedByte > 0xFF;
    }
}