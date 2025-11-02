namespace GameboyDotnet.Memory.BuildingBlocks;

/// <summary>
/// A switchable bank that uses CgbState to determine the current bank
/// Used for VRAM and WRAM banking in CGB mode
/// </summary>
public class CgbAwareSwitchableBank : SwitchableBank
{
    private readonly CgbState _cgbState;
    private readonly bool _isCgbVram;
    
    public CgbAwareSwitchableBank(int startAddress, int endAddress, string name, int bankSizeInBytes, 
        int numberOfBanks, CgbState cgbState, bool isCgbVram) 
        : base(startAddress, endAddress, name, bankSizeInBytes, numberOfBanks)
    {
        _cgbState = cgbState;
        _isCgbVram = isCgbVram;
    }
    
    /// <summary>
    /// Gets the current bank based on CGB state
    /// </summary>
    private int GetCurrentBank()
    {
        if (_isCgbVram)
        {
            // VRAM banking: use VBK register
            return _cgbState.CurrentVramBank;
        }
        else
        {
            // WRAM banking: use SVBK register
            return _cgbState.CurrentWramBank;
        }
    }

    public override byte ReadByte(ref ushort address)
    {
        int bank = GetCurrentBank();
        return MemorySpace[bank * BankSizeInBytes + address - StartAddress];
    }
    
    public override void WriteByte(ref ushort address, ref byte value)
    {
        int bank = GetCurrentBank();
        MemorySpace[bank * BankSizeInBytes + address - StartAddress] = value;
    }
    
    public override void IncrementByte(ref ushort memoryAddress)
    {
        int bank = GetCurrentBank();
        MemorySpace[bank * BankSizeInBytes + memoryAddress - StartAddress]++;
    }

    public override void DecrementByte(ref ushort memoryAddress)
    {
        int bank = GetCurrentBank();
        MemorySpace[bank * BankSizeInBytes + memoryAddress - StartAddress]--;
    }
}
