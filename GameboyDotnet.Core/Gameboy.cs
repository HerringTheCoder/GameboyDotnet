using GameboyDotnet.Components;
using GameboyDotnet.PPU;
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
        Ppu = new Ppu(new Lcd(Cpu.MemoryController));
    }

    public void LoadProgram(FileStream stream)
    {
        Cpu.MemoryController.LoadProgram(stream);
        _logger.LogDebug("Program loaded successfully");
    }

    public async Task RunAsync(
       CancellationToken ctsToken)
    {
        while (!ctsToken.IsCancellationRequested)
        {
            try
            {
                var tStates = Cpu.ExecuteNextOperation();
                TimaTimer.CheckAndIncrementTimer(ref tStates, Cpu.MemoryController);
                DivTimer.CheckAndIncrementTimer(ref tStates, Cpu.MemoryController);
                Ppu.CheckAndPush(tStates, Cpu.MemoryController);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running the Gameboy");
                throw;
            }
        }
       
    }
}