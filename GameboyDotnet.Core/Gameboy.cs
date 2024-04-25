using System.Diagnostics;
using GameboyDotnet.Graphics;
using GameboyDotnet.Processor;
using GameboyDotnet.Timers;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet;

public partial class Gameboy
{
    private readonly ILogger<Gameboy> _logger;
    public Cpu Cpu { get; }
    public Ppu Ppu { get; }
    public MainTimer TimaTimer { get; } = new();
    public DividerTimer DivTimer { get; } = new();

    public Gameboy(ILogger<Gameboy> logger)
    {
        _logger = logger;
        Cpu = new Cpu(logger);
        Ppu = new Ppu(Cpu.MemoryController);
    }

    public void LoadProgram(FileStream stream)
    {
        Cpu.MemoryController.LoadProgram(stream);
        _logger.LogInformation("Program loaded successfully");
    }

    public Task RunAsync(
        CancellationToken ctsToken)
    {
        var frameTimeTicks = TimeSpan.FromMilliseconds(16.75).Ticks;
        
        var cyclesPerFrame = Cycles.CyclesPerFrame;
        var currentCycles = 0;

        while (!ctsToken.IsCancellationRequested)
        {
            try
            {
                var startTime = Stopwatch.GetTimestamp();
                var targetTime = startTime + frameTimeTicks;
                while (currentCycles < cyclesPerFrame)
                {
                    var tStates = Cpu.ExecuteNextOperation();
                    Ppu.PushPpuCycles(tStates);
                    TimaTimer.CheckAndIncrementTimer(ref tStates, Cpu.MemoryController);
                    DivTimer.CheckAndIncrementTimer(ref tStates, Cpu.MemoryController);
                    currentCycles += tStates;
                }
                UpdateJoypadState();

                currentCycles -= cyclesPerFrame;
                DisplayUpdated.Invoke(this, EventArgs.Empty);

                // var endTime = Stopwatch.GetTimestamp();
                // while (Stopwatch.GetTimestamp() < targetTime)
                // {
                //     //Wait in a tight loop for until target time is reached
                // }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running the Gameboy");
                throw;
            }
        }

        return Task.CompletedTask;
    }
}