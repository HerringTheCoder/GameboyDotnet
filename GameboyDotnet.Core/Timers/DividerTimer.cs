using GameboyDotnet.Components;
using GameboyDotnet.Memory;

namespace GameboyDotnet.Timers;

public class DividerTimer
{
    private int _dividerCycleCounter;
    
    internal void CheckAndIncrementTimer(ref byte tStates, MemoryController memoryController)
    {
        _dividerCycleCounter += tStates;
        if (_dividerCycleCounter >= Cycles.DividerCycles)
        {
            _dividerCycleCounter -= Cycles.DividerCycles;
            memoryController.IncrementByte(Constants.DIVRegister);
        }
    }
}