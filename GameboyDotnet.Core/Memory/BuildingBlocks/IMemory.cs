namespace GameboyDotnet.Components.Memory.BuildingBlocks;

public interface IMemory
{
    byte ReadByte(ref ushort address);
    void WriteByte(ref ushort address, ref byte value);
    ushort ReadWord(ref ushort address);
    void WriteWord(ref ushort address, ref ushort value);
}