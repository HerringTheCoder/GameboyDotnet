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
    public byte? PressedKeyValue { get; set; }

    public Gameboy(ILogger<Gameboy> logger, bool isTestEnvironment = false)
    {
        _logger = logger;
        Cpu = new Cpu(logger, isTestEnvironment);
        Ppu = new Ppu(Cpu.MemoryController);
    }

    public void LoadProgram(FileStream stream)
    {
        Cpu.MemoryController.LoadProgram(stream);
        _logger.LogDebug("Program loaded successfully");
    }

    public async Task RunAsync(
        CancellationToken ctsToken)
    {
        var sw = new Stopwatch();
        var cyclesPerFrame = Cycles.CyclesPerFrame;
        var currentCycles = 0;

        while (!ctsToken.IsCancellationRequested)
        {
            try
            {
                sw.Restart();
                while (currentCycles < cyclesPerFrame)
                {
                    var tStates = Cpu.ExecuteNextOperation();
                    Ppu.PushPpuCycles(tStates);
                    TimaTimer.CheckAndIncrementTimer(ref tStates, Cpu.MemoryController);
                     DivTimer.CheckAndIncrementTimer(ref tStates, Cpu.MemoryController);
                    currentCycles += tStates;
                }
                
                DisplayUpdated.Invoke(this, EventArgs.Empty);
                sw.Stop();
                currentCycles -= cyclesPerFrame;
            
                // if (sw.Elapsed < TimeSpan.FromMilliseconds(16.7))
                //     await Task.Delay(TimeSpan.FromMilliseconds(16.7) - sw.Elapsed, ctsToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running the Gameboy");
                throw;
            }
        }
    }
}