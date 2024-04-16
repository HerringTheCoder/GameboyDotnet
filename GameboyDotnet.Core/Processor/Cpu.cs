using GameboyDotnet.Components;
using GameboyDotnet.Components.Cpu;
using GameboyDotnet.Extensions;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Processor;

public partial class Cpu
{
    internal CpuRegister Register { get; set; } = new();
    internal MemoryController MemoryController { get; }
    public bool IsHalted { get; set; }
    private int _tStatesCounter = 0;
    private readonly ILogger<Gameboy> _logger;

    private readonly bool _isTestEnvironment;

    public Cpu(ILogger<Gameboy> logger, bool isTestEnvironment)
    {
        _logger = logger;
        _isTestEnvironment = isTestEnvironment;
        MemoryController = new MemoryController(logger, isTestEnvironment);
    }

    public byte ExecuteNextOperation()
    {
        var interruptFlags = MemoryController.ReadByte(Constants.IFRegister);
        var interruptEnable = MemoryController.ReadByte(Constants.IERegister);
        var interrupt = interruptFlags & interruptEnable;
        if (interrupt != 0)
        {
            _logger.LogDebug("Interrupt detected: {interrupt:X2}", interrupt);
            HandleInterrupt(interrupt);
        }
        
        if(IsHalted)
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

        if (opCode != 0xFB && Register.IMEPending)
        {
            Register.IMEPending = false;
            Register.InterruptsMasterEnabled = true;
        }

        Register.PC += operationSize.instructionBytesLength;
        
        return operationSize.durationTStates;
    }

    private void HandleInterrupt(int interrupt)
    {
        IsHalted = false;
        if (Register.InterruptsMasterEnabled)
        {
            Register.InterruptsMasterEnabled = false;
            MemoryController.WriteByte(Constants.IFRegister, 0);
            PushStack(Register.PC);
            Register.PC = interrupt switch
            {
                0x01 => 0x0040,
                0x02 => 0x0048,
                0x04 => 0x0050,
                0x08 => 0x0058,
                0x10 => 0x0060,
                _ => throw new ArgumentOutOfRangeException(nameof(interrupt), interrupt, "Invalid interrupt")
            };
        }
    }

    

    private void LogTestOutput(StreamWriter writer)
    {
        var pcmem0 = MemoryController.ReadByte(Register.PC);
        var pcmem1 = MemoryController.ReadByte(Register.PC.Add(1));
        var pcmem2 = MemoryController.ReadByte(Register.PC.Add(2));
        var pcmem3 = MemoryController.ReadByte(Register.PC.Add(3));

        writer.WriteLine(
            $"A:{Register.A:X2} " +
            $"F:{Register.F:X2} " +
            $"B:{Register.B:X2} " +
            $"C:{Register.C:X2} " +
            $"D:{Register.D:X2} " +
            $"E:{Register.E:X2} " +
            $"H:{Register.H:X2} " +
            $"L:{Register.L:X2} " +
            $"SP:{Register.SP:X4} " +
            $"PC:{Register.PC:X4} " +
            $"PCMEM:{pcmem0:X2},{pcmem1:X2},{pcmem2:X2},{pcmem3:X2}");
    }

    private bool CheckCondition(ref byte opCode)
    {
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
}