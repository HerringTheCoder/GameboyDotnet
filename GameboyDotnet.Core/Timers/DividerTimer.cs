using GameboyDotnet.Components;

namespace GameboyDotnet.Timers;

public class DividerTimer
{
    private int _dividerCycleCounter;
    
    internal void CheckAndIncrementTimer(ref byte tStates, MemoryController memoryController)
    {
        _dividerCycleCounter += tStates;
        if (_dividerCycleCounter >= Constants.DividerCycles)
        {
            _dividerCycleCounter -= Constants.DividerCycles;
            memoryController.IncrementByte(Constants.DIVRegister);
        }
    }
}