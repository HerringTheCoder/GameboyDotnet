using GameboyDotnet.Components;
using Microsoft.Extensions.Logging;
using GameboyDotnet.Processor;

namespace GameboyDotnet;

public partial class Gameboy
{
    private readonly ILogger<Gameboy> _logger;
    public Cpu Cpu { get; }
    public Lcd Lcd { get; } = new();
    public byte? PressedKeyValue { get; set; }

    public Gameboy(ILogger<Gameboy> logger, bool isTestEnvironment = false)
    {
        _logger = logger;
        Cpu = new Cpu(logger, isTestEnvironment);
    }

    public void LoadProgram(FileStream stream)
    {
        Cpu.MemoryController.LoadProgram(stream);
        _logger.LogDebug("Program loaded successfully");
    }

    public async Task RunAsync(
        int cyclesPerSecond, 
        int operationsPerCycle, 
        int ticksPerSecond,
        CancellationToken ctsToken)
    {
        try
        {
            await Task.WhenAll(
                CpuThread(cyclesPerSecond, operationsPerCycle, ctsToken),
                DisplayThread(ticksPerSecond, ctsToken)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running the Gameboy");
            throw;
        }
    }
}