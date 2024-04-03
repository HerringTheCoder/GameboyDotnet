namespace GameboyDotnet;

public partial class Gameboy
{
    public async Task CpuThread(int cyclesPerSecond, int operationsPerCycle, CancellationToken ctsToken)
    {
        var delayTime = TimeSpan.FromSeconds(1.0 / cyclesPerSecond);
        while (!ctsToken.IsCancellationRequested)
        {
            if(Cpu.IsHalted)
            {
                await Task.Delay(delayTime, ctsToken);
                continue;
            }
            
            for(var i = 0; i < operationsPerCycle; i++)
            {
                Cpu.ExecuteNextOperation();
            }
            await Task.Delay(delayTime, ctsToken);
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