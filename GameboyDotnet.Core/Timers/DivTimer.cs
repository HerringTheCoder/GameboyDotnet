using GameboyDotnet.Memory;

namespace GameboyDotnet.Timers;

public class DivTimer(MemoryController memoryController)
{
    private int _dividerCycleCounter;
    
    internal void CheckAndIncrementTimer(ref byte tStates)
    {
        _dividerCycleCounter += tStates;
        
        if (_dividerCycleCounter >= Cycles.DividerCycles)
        { 
            _dividerCycleCounter -= Cycles.DividerCycles;
            
            memoryController.IncrementByte(Constants.DIVRegister);
        }
    }
}