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
    public bool IsFrameLimiterEnabled;
    internal bool IsMemoryDumpingActive;

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
        IsFrameLimiterEnabled = frameLimitEnabled;
        var frameTimeTicks = TimeSpan.FromMilliseconds(16.75).Ticks;
        
        var cyclesPerFrame = Cycles.CyclesPerFrame;
        var currentCycles = 0;
        var frameCounter = 0;

        while (!ctsToken.IsCancellationRequested)
        {
            if (IsMemoryDumpingActive && frameCounter == 0)
            {
                Task.Delay(TimeSpan.FromSeconds(1), ctsToken).RunSynchronously();
                continue;
            }

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
                
                frameCounter++;
                currentCycles -= cyclesPerFrame;
                DisplayUpdated.Invoke(this, EventArgs.Empty);
                if(frameCounter % 60 == 0)
                {
                    frameCounter = 0;
                }
                
                if(IsFrameLimiterEnabled)
                {
                    while (Stopwatch.GetTimestamp() < targetTime)
                    {
                        //Wait in a tight loop until the target time is reached
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

    public void SwitchFramerateLimiter()
    {
        IsFrameLimiterEnabled = !IsFrameLimiterEnabled;
        _logger.LogWarning("Frame limiter is now '{IsFrameLimiterEnabled}'", IsFrameLimiterEnabled ? "enabled" : "disabled");
    }
}