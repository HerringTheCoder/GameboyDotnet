using GameboyDotnet.Extensions;
using GameboyDotnet.Memory.BuildingBlocks;

namespace GameboyDotnet.Memory.Mbc;

// https://gbdev.io/pandocs/MBC5.html

/// <summary>
/// MBC5 - Memory Bank Controller 5
/// Can map up to 64 Mbits (8 MiB) of ROM (up to 512 banks)
/// Supports RAM sizes of 8 KiB, 32 KiB, and 128 KiB (up to 16 banks)
/// First MBC guaranteed to work properly with GBC Double Speed mode
/// </summary>
public class Mbc5(string name, int bankSizeInBytes, int numberOfBanks, int ramBankCount)
    : MemoryBankController(name, bankSizeInBytes, numberOfBanks, ramBankCount)
{
    private int _romBankLower;      // 8 least significant bits (0x2000-0x2FFF)
    private int _romBankUpper;      // 9th bit (0x3000-0x3FFF)
    private int _ramBankNumber;     // RAM bank 0x00-0x0F
    private bool _rumbleEnabled;    // Bit 3 of RAM bank register (for cartridges with rumble)

    public override void WriteByte(ref ushort address, ref byte value)
    {
        switch (address)
        {
            case <= 0x1FFF:
                // RAM Enable: lower 4 bits must be 0xA
                // Note: Actual MBCs enable RAM when bottom 4 bits equal $A
                ExternalRamEnabled = (value & 0x0F) == 0x0A;
                break;
                
            case <= 0x2FFF:
                // ROM Bank Number - 8 least significant bits
                _romBankLower = value;
                UpdateRomBank();
                break;
                
            case <= 0x3FFF:
                // ROM Bank Number - 9th bit (bit 0 of value)
                _romBankUpper = value & 0x01;
                UpdateRomBank();
                break;
                
            case <= 0x5FFF:
                // RAM Bank Number (0x00-0x0F)
                // Bit 3 is used for rumble on cartridges with rumble motor
                _ramBankNumber = value & 0x0F;
                _rumbleEnabled = (value & 0x08) != 0;
                
                if (ExternalRam.NumberOfBanks > 0)
                {
                    ExternalRam.CurrentBank = _ramBankNumber % ExternalRam.NumberOfBanks;
                }
                break;
                
            case >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd:
                // Write to external RAM
                if (ExternalRamEnabled && ExternalRam.NumberOfBanks > 0)
                {
                    ExternalRam.WriteByte(ref address, ref value);
                }
                break;
        }
    }

    public override byte ReadByte(ref ushort address)
    {
        return address switch
        {
            // 0000-3FFF: ROM Bank 00 (always bank 0)
            <= BankAddress.RomBank0End 
                => MemorySpace[address - StartAddress],
                
            // A000-BFFF: External RAM Bank
            >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd 
                => ReadExternalRam(ref address),
                
            // 4000-7FFF: Switchable ROM Bank (0x00-0x1FF, bank 0 is actually bank 0)
            _ => MemorySpace[(CurrentBank % NumberOfBanks) * BankSizeInBytes + (address - BankAddress.RomBankNnStart)]
        };
    }
    
    private byte ReadExternalRam(ref ushort address)
    {
        if (!ExternalRamEnabled || ExternalRam.NumberOfBanks == 0)
            return 0xFF;
            
        return ExternalRam.ReadByte(ref address);
    }
    
    private void UpdateRomBank()
    {
        // Combine 9-bit ROM bank number: upper bit (bit 8) + lower byte (bits 0-7)
        // MBC5 can access banks 0x000-0x1FF (0-511)
        // Unlike other MBCs, bank 0 is actually bank 0 (not treated as bank 1)
        int romBank = (_romBankUpper << 8) | _romBankLower;
        CurrentBank = romBank % NumberOfBanks;
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
    
    /// <summary>
    /// Gets whether the rumble motor is enabled (for cartridges with rumble)
    /// </summary>
    public bool IsRumbleEnabled => _rumbleEnabled;
}
