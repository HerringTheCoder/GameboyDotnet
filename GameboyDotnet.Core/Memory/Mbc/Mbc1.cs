using GameboyDotnet.Extensions;
using GameboyDotnet.Memory.BuildingBlocks;

namespace GameboyDotnet.Memory.Mbc;

//https://gbdev.io/pandocs/MBC1.html

public class Mbc1( string name, int bankSizeInBytes, int numberOfBanks, int ramBankCount) 
    : MemoryBankController(name, bankSizeInBytes, numberOfBanks, ramBankCount)
{
    private int _romBankLower;  // 5-bit register (0x2000-0x3FFF)
    private int _bankUpper;      // 2-bit register (0x4000-0x5FFF)
    
    public override void WriteByte(ref ushort address, ref byte value)
    {
        switch (address)
        {
            case <= 0x1FFF:
                // RAM Enable: lower 4 bits must be 0xA
                ExternalRamAndRtcEnabled = (value & 0x0F) == 0x0A;
                break;
                
            case <= BankAddress.RomBank0End:
                // 2000 - 3FFF - ROM Bank Number (lower 5 bits)
                _romBankLower = value & 0x1F;
                UpdateRomBank();
                break;
                
            case <= 0x5FFF:
                // RAM Bank Number OR Upper bits of ROM Bank Number
                _bankUpper = value & 0x03;
                UpdateRomBank();
                UpdateRamBank();
                break;
                
            case <= BankAddress.RomBankNnEnd:
                // Banking Mode Select
                RomBankingMode = value & 0x01;
                UpdateRomBank();
                UpdateRamBank();
                break;
            
            case >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd when ExternalRamAndRtcEnabled:
                ExternalRam.WriteByte(ref address, ref value);
                break;
        }
    }

    public override byte ReadByte(ref ushort address)
    {
        return address switch
        {
            // Mode 1: 0000-3FFF uses upper bits for bank selection
            <= BankAddress.RomBank0End when RomBankingMode == 1 && NumberOfBanks >= 64 
                => MemorySpace[((_bankUpper << 5) % NumberOfBanks) * BankSizeInBytes + (address - StartAddress)],
            // Mode 0 or small ROM: 0000-3FFF always bank 0
            <= BankAddress.RomBank0End 
                => MemorySpaceView[address - StartAddress],
            // External RAM
            >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd 
                => ExternalRamAndRtcEnabled 
                    ? ExternalRam.ReadByte(ref address) 
                    : (byte)0xFF,
            // 4000-7FFF: switchable ROM bank
            _ => MemorySpaceView[CurrentBank * BankSizeInBytes + (address - BankAddress.RomBankNnStart)]
        };
    }
    
    private void UpdateRomBank()
    {
        // Calculate effective ROM bank for 4000-7FFF region
        // Mode 0: upper bits apply to ROM
        // Mode 1: upper bits don't apply to ROM (ROM limited to banks 0-31)
        var effectiveBank = (RomBankingMode == 0 ? (_bankUpper << 5) : 0) | _romBankLower;
        
        // 00->01 translation: if lower 5 bits are 0, treat as 1
        if (_romBankLower == 0)
            effectiveBank |= 1;
        
        CurrentBank = effectiveBank % NumberOfBanks;
    }
    
    private void UpdateRamBank()
    {
        // Mode 0: RAM locked to bank 0
        // Mode 1: RAM uses upper bits register
        if (ExternalRam.NumberOfBanks > 1)
        {
            ExternalRam.CurrentBank = RomBankingMode == 1 
                ? _bankUpper % ExternalRam.NumberOfBanks 
                : 0;
        }
    }
    
    public override void IncrementByte(ref ushort memoryAddress)
    {
        var newValue = ReadByte(ref memoryAddress).Add(1);
        WriteByte(ref memoryAddress, ref newValue);
    }
    
    public override void DecrementByte(ref ushort memoryAddress)
    {
        var newValue = ReadByte(ref memoryAddress).Subtract(1);
        WriteByte(ref memoryAddress, ref newValue);
    }
}