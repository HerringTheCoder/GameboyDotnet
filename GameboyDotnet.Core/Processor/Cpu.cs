using System.Numerics;
using GameboyDotnet.Components.Cpu;
using GameboyDotnet.Extensions;
using GameboyDotnet.Memory;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Processor;

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

    public byte ExecuteNextOperation()
    {
        bool interruptHandled = HandleInterrupt();
        
        if (IsHalted)
        {
            return 1;
        }

        var opCode = MemoryController.ReadByte(Register.PC);
        var operationBlock = (opCode & 0b11000000) >> 6;

        var operationSize = operationBlock switch
        {
            0x0 => ExecuteBlock0(ref opCode),
            0x1 => ExecuteBlock1(ref opCode),
            0x2 => ExecuteBlock2(ref opCode),
            0x3 => ExecuteBlock3(ref opCode),
            _ => throw new NotImplementedException($"Operation {opCode:X2} not implemented")
        };

        if (Register.IMEPending && opCode != 0xFB)
        {
            Register.IMEPending = false;
            Register.InterruptsMasterEnabled = true;
        }

        Register.PC += operationSize.instructionBytesLength;
        
        return (byte)(operationSize.durationTStates + (interruptHandled ? 5 : 0));
    }

    private bool HandleInterrupt()
    {
        var interruptFlags = MemoryController.ReadByte(Constants.IFRegister);
        var interruptEnable = MemoryController.ReadByte(Constants.IERegister);
        var interrupt = (byte)(interruptFlags & interruptEnable);

        if (interrupt == 0 || !Register.InterruptsMasterEnabled) //TODO: https://gbdev.io/pandocs/halt.html
            return false;

        IsHalted = false;
        int interruptIndex = BitOperations.TrailingZeroCount(interrupt);
        Register.InterruptsMasterEnabled = false;
        MemoryController.WriteByte(Constants.IFRegister, interruptFlags.ClearBit(interruptIndex));
        PushStack(Register.PC);

        Register.PC = (1 << interruptIndex) switch
        {
            0x01 => 0x0040,
            0x02 => 0x0048,
            0x04 => 0x0050,
            0x08 => 0x0058,
            0x10 => 0x0060,
            _ => throw new ArgumentOutOfRangeException(nameof(interrupt), interrupt, "Invalid interrupt")
        };

        return true;
    }

    private bool CheckCondition(ref byte opCode)
    {
        if(_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("{opcode:X2} - Checking condition flag", opCode);
        
        return opCode.GetCondition() switch
        {
            0b00 => !Register.ZeroFlag,
            0b01 => Register.ZeroFlag,
            0b10 => !Register.CarryFlag,
            0b11 => Register.CarryFlag,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    
    // private void LogTestOutput(StreamWriter writer)
    // {
    //     var pcmem0 = MemoryController.ReadByte(Register.PC);
    //     var pcmem1 = MemoryController.ReadByte(Register.PC.Add(1));
    //     var pcmem2 = MemoryController.ReadByte(Register.PC.Add(2));
    //     var pcmem3 = MemoryController.ReadByte(Register.PC.Add(3));
    //
    //     writer.WriteLine(
    //         $"A:{Register.A:X2} " +
    //         $"F:{Register.F:X2} " +
    //         $"B:{Register.B:X2} " +
    //         $"C:{Register.C:X2} " +
    //         $"D:{Register.D:X2} " +
    //         $"E:{Register.E:X2} " +
    //         $"H:{Register.H:X2} " +
    //         $"L:{Register.L:X2} " +
    //         $"SP:{Register.SP:X4} " +
    //         $"PC:{Register.PC:X4} " +
    //         $"PCMEM:{pcmem0:X2},{pcmem1:X2},{pcmem2:X2},{pcmem3:X2}");
    // }
}