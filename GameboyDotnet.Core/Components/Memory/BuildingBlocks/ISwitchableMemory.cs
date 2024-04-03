namespace GameboyDotnet.Components.Memory.BuildingBlocks;

public interface ISwitchableMemory : IMemory
{
    public int CurrentBank { get; set; }
}