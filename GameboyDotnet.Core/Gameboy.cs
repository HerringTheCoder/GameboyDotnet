using GameboyDotnet.Components;
using GameboyDotnet.Components.Cpu;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet;

public partial class Gameboy
{
    private readonly ILogger<Gameboy> _logger;
    public Cpu Cpu { get; }
    public Lcd Lcd { get; } = new();
    public byte? PressedKeyValue { get; set; }
    
    
    public Gameboy(ILogger<Gameboy> logger)
    {
        _logger = logger;
        Cpu = new Cpu(logger);
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
        catch (AggregateException ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }
}