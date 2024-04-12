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
    private readonly ILogger<Gameboy> _logger;
    
    private readonly bool _isTestEnvironment;
    private int _testLogCounter = 0;

    public Cpu(ILogger<Gameboy> logger, bool isTestEnvironment)
    {
        _logger = logger;
        _isTestEnvironment = isTestEnvironment;
        MemoryController = new MemoryController(logger, isTestEnvironment);
    }

    public void ExecuteNextOperation(StreamWriter? testWriter = null)
    {
        if(_isTestEnvironment)
            LogTestOutput(testWriter ?? throw new ArgumentNullException(nameof(testWriter), "Test writer needs to be defined in test mode"));
        
        var opCode = MemoryController.ReadByte(Register.PC);
        
        //Idea of operation blocks is based on: https://gbdev.io/pandocs/CPU_Instruction_Set.html
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
    }
    
    private void LogTestOutput(StreamWriter writer)
    {
        _testLogCounter++;
        if (_testLogCounter % 500 == 0)
        {
            Console.WriteLine($"Operation number: {_testLogCounter}");
        }
        
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