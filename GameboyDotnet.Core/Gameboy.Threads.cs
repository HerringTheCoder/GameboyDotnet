using Microsoft.Extensions.Logging;

namespace GameboyDotnet;

public partial class Gameboy
{
    public async Task CpuThread(int cyclesPerSecond, int operationsPerCycle, CancellationToken ctsToken)
    {
        try
        {
            var delayTime = TimeSpan.FromSeconds(1.0 / cyclesPerSecond);
            while (!ctsToken.IsCancellationRequested)
            {
                if (Cpu.IsHalted)
                {
                    await Task.Delay(delayTime, ctsToken);
                    continue;
                }
                Cpu.ExecuteNextOperation();

                // await Task.Delay(delayTime, ctsToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running the CPU");
            throw;
        }
    }
    
    public async Task DisplayThread(int ticksPerSecond, CancellationToken ctsToken)
    {
        var delayTime = TimeSpan.FromSeconds(1.0 / ticksPerSecond);
        while (!ctsToken.IsCancellationRequested)
        {
            OnDisplayUpdated(EventArgs.Empty);
            await Task.Delay(delayTime, ctsToken);
        }
    }
}