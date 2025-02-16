using GameboyDotnet.Memory;

namespace GameboyDotnet.Sound;

public class ApuRegisters(MemoryController memoryController)
{
    /// <summary>
    /// Bit 7 Audio On/Off
    /// </summary>
    public byte NR52AudioMasterControl => memoryController.IoRegisters.MemorySpaceView[0x26];
    
    /// <summary>
    /// Bits 7-4 CH4-CH1 left, Bits 3-0 CH4-CH1 right 
    /// </summary>
    public byte NR51SoundPanning => memoryController.IoRegisters.MemorySpaceView[0x25];

    /// <summary>
    /// 7 - VIN left (safe to ignore for now), (654) - left volume, 3- VIN right, (210) - Right Volume
    /// Value of 0 is treated as 1 (very quiet), value of 7 is then like full 8 (no reduction)
    /// </summary>
    public byte NR50MasterVolume => memoryController.IoRegisters.MemorySpaceView[0x24];
}