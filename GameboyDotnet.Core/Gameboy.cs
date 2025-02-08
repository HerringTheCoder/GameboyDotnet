using System.Diagnostics;
using GameboyDotnet.Common;
using GameboyDotnet.Graphics;
using GameboyDotnet.Processor;
using GameboyDotnet.Timers;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet;

public partial class Gameboy
{
    private ILogger<Gameboy> _logger;
    public Cpu Cpu { get; }
    public Ppu Ppu { get; }
    public MainTimer TimaTimer { get; } = new();
    public DividerTimer DivTimer { get; } = new();
    public bool IsDebugMode { get; private set; }

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

    public Task RunAsync(bool frameLimitEnabled, CancellationToken ctsToken)
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

                // if (frameLimitEnabled)
                // {
                //     var remainingTime = targetTime - Stopwatch.GetTimestamp();
                //     if (remainingTime > 0)
                //     {
                //         SpinWait.SpinUntil(() => Stopwatch.GetTimestamp() >= targetTime);
                //     }
                // }
                if(frameLimitEnabled)
                {
                    while (Stopwatch.GetTimestamp() < targetTime)
                    {
                        //Wait in a tight loop for until target time is reached
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running the Gameboy");
                throw;
            }
        }

        return Task.CompletedTask;
    }

    public void SwitchDebugMode()
    {
        _logger.LogInformation("Switching debug mode to {IsDebugMode}", !IsDebugMode);
        IsDebugMode = !IsDebugMode;
        _logger = LoggerHelper.GetLogger<Gameboy>(IsDebugMode ? LogLevel.Debug : LogLevel.Information);
    }
}