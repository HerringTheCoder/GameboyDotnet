using GameboyDotnet.Memory;

namespace GameboyDotnet.Timers;

public class DivTimer(MemoryController memoryController)
{
    internal int DividerCycleCounter;

    internal void CheckAndIncrementTimer(ref byte tStates)
    {
        DividerCycleCounter += tStates;
        
        if (DividerCycleCounter >= Cycles.DividerCycles)
        { 
            DividerCycleCounter -= Cycles.DividerCycles;
            
            memoryController.IncrementByte(Constants.DIVRegister);
        }
    }
}