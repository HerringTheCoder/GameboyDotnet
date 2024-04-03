namespace GameboyDotnet.Components.Memory.BuildingBlocks;

public class SwitchableBank : FixedBank, ISwitchableMemory
{
    public int CurrentBank { get; set; }
    public required int BankSizeInBytes { get; init; }
    public required int NumberOfBanks { get; init; }

    public SwitchableBank()
    {
        MemorySpace = new byte[BankSizeInBytes * NumberOfBanks];
    }

    public override byte ReadByte(ref ushort address)
    {
        return MemorySpace[CurrentBank * BankSizeInBytes + address - StartAddress];
    }
    
    public override void WriteByte(ref ushort address, ref byte value)
    {
        MemorySpace[CurrentBank * BankSizeInBytes + address - StartAddress] = value;
    }
    
    public override ushort ReadWord(ref ushort address)
    {
        return (ushort)(MemorySpace[CurrentBank * BankSizeInBytes + address - StartAddress] | (MemorySpace[CurrentBank * BankSizeInBytes + address - StartAddress + 1] << 8));
    }
    
    public override void WriteWord(ref ushort address, ref ushort value)
    {
        MemorySpace[CurrentBank * BankSizeInBytes + address - StartAddress] = (byte)(value & 0xFF);
        MemorySpace[CurrentBank * BankSizeInBytes + address - StartAddress + 1] = (byte)(value >> 8);
    }
    
    public override void IncrementByte(ref ushort memoryAddress)
    {
        MemorySpace[CurrentBank * BankSizeInBytes + memoryAddress - StartAddress]++;
    }

    public override void DecrementByte(ref ushort memoryAddress)
    {
        MemorySpace[CurrentBank * BankSizeInBytes + memoryAddress - StartAddress]--;
    }
}