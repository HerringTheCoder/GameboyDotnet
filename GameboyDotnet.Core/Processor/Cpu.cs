using GameboyDotnet.Components.Cpu;
using GameboyDotnet.Extensions;
using GameboyDotnet.Memory;
using GameboyDotnet.Sound;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Processor;

public partial class Cpu
{
    internal CpuRegister Register { get; set; } = new();
    internal MemoryController MemoryController { get; }
    public bool IsHalted { get; set; }
    private readonly ILogger<Gameboy> _logger;

    public Cpu(ILogger<Gameboy> logger, MemoryController memoryController)
    {
        _logger = logger;
        MemoryController = memoryController;
    }

    public byte ExecuteNextOperation()
    {
        bool interruptHandled = HandleInterrupt();
        
        if (IsHalted)
        {
            return 1;
        }

        var opCode = MemoryController.ReadByte(address: Register.PC);
        var operationBlock = (opCode & 0b11000000) >> 6;

        var operationSize = operationBlock switch
        {
            0x0 => ExecuteBlock0(ref opCode),
            0x1 => ExecuteBlock1(ref opCode),
            0x2 => ExecuteBlock2(ref opCode),
            0x3 => ExecuteBlock3(ref opCode),
            _ => throw new NotImplementedException($"Operation {opCode:X2} not implemented")
        };

        Register.PC += operationSize.instructionBytesLength;

        // EI has a 1-instruction delay: IME is enabled after the next instruction completes
        if (Register.IMEPending)
        {
            Register.IMEPending = false;
            Register.InterruptsMasterEnabled = true;
        }
        
        return (byte)(operationSize.durationTStates + (interruptHandled ? 20 : 0));
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