using System.Diagnostics;
using GameboyDotnet.Common;
using GameboyDotnet.Graphics;
using GameboyDotnet.Memory;
using GameboyDotnet.Processor;
using GameboyDotnet.Sound;
using GameboyDotnet.Timers;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet;

public partial class Gameboy
{
    private ILogger<Gameboy> _logger;
    public MemoryController MemoryController { get; }
    public Cpu Cpu { get; }
    public Ppu Ppu { get; }
    public Apu Apu { get; }
    public TimaTimer TimaTimer { get; }
    public DivTimer DivTimer { get; }
    public bool IsFrameLimiterEnabled;
    public bool IsMemoryDumpRequested;

    public Gameboy(ILogger<Gameboy> logger)
    {
        _logger = logger;
        Apu = new Apu();
        MemoryController = new MemoryController(logger, Apu);
        Cpu = new Cpu(logger, MemoryController);
        Ppu = new Ppu(MemoryController);
        TimaTimer = new TimaTimer(MemoryController);
        DivTimer = new DivTimer(MemoryController);
    }

    public void LoadProgram(FileStream stream)
    {
        Cpu.MemoryController.LoadProgram(stream);
        _logger.LogInformation("Program loaded successfully");
    }

    public Task RunAsync(bool frameLimitEnabled, CancellationToken ctsToken)
    {
        IsFrameLimiterEnabled = frameLimitEnabled;
        var frameTimeTicks = TimeSpan.FromMilliseconds(16.75).Ticks; //~59.7 Hz
        
        var cyclesPerFrame = Cycles.CyclesPerFrame;
        var currentCycles = 0;

        while (!ctsToken.IsCancellationRequested)
        {
            try
            {
                var startTime = Stopwatch.GetTimestamp();
                var targetTime = startTime + frameTimeTicks;
                
                while (currentCycles < cyclesPerFrame) //70224(Gameboy) or 140448 (Gameboy Color)
                {
                    var tStates = Cpu.ExecuteNextOperation();
                    Ppu.PushPpuCycles(tStates);
                    TimaTimer.CheckAndIncrementTimer(ref tStates);
                    DivTimer.CheckAndIncrementTimer(ref tStates);
                    Apu.PushApuCycles(ref tStates);
                    currentCycles += tStates;
                }
                
                UpdateJoypadState();
                currentCycles -= cyclesPerFrame;
                Ppu.FrameBuffer.EnqueueFrame(Ppu.Lcd);
                
                if (IsMemoryDumpRequested)
                {
                    DumpMemory();
                    continue;
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