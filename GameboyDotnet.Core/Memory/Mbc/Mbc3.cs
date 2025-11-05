using GameboyDotnet.Extensions;
using GameboyDotnet.Memory.BuildingBlocks;

namespace GameboyDotnet.Memory.Mbc;

//https://gbdev.io/pandocs/MBC3.html

public class Mbc3(string name, int bankSizeInBytes, int numberOfBanks, int ramBankCount)
    : MemoryBankController(name, bankSizeInBytes, numberOfBanks, ramBankCount)
{
    private int _romBankNumber;           // 7-bit register (0x2000-0x3FFF)
    private int _ramBankOrRtcSelect;      // RAM bank 0x00-0x07 or RTC register 0x08-0x0C
    private byte _latchClockData;         // For tracking the $00->$01 latch sequence
    
    // RTC Internal registers (writable, ticking)
    private byte _rtcSeconds;
    private byte _rtcMinutes;
    private byte _rtcHours;
    private byte _rtcDaysLow;
    private byte _rtcDaysHigh;
    
    // RTC Latched registers (readable, frozen until next latch)
    private byte _rtcSecondsLatched;
    private byte _rtcMinutesLatched;
    private byte _rtcHoursLatched;
    private byte _rtcDaysLowLatched;
    private byte _rtcDaysHighLatched;

    public override void WriteByte(ref ushort address, ref byte value)
    {
        switch (address)
        {
            case <= 0x1FFF:
                // RAM and Timer Enable: lower 4 bits must be 0xA
                ExternalRamEnabled = (value & 0x0F) == 0x0A;
                break;
                
            case <= 0x3FFF:
                // ROM Bank Number (7 bits)
                _romBankNumber = value & 0x7F;
                UpdateRomBank();
                break;
                
            case <= 0x5FFF:
                // RAM Bank Number (0x00-0x07) or RTC Register Select (0x08-0x0C)
                if (value is <= 0x07 or >= 0x08 and <= 0x0C)
                    _ramBankOrRtcSelect = value;
                break;
                
            case <= 0x7FFF:
                // Latch Clock Data: Write $00 then $01 to latch
                if (_latchClockData == 0x00 && value == 0x01)
                    LatchRtcRegisters();
                _latchClockData = value;
                break;
                
            case >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd:
                WriteRamOrRtc(ref address, ref value);
                break;
        }
    }
    
    private void WriteRamOrRtc(ref ushort address, ref byte value)
    {
        if (!ExternalRamEnabled)
            return;
            
        switch (_ramBankOrRtcSelect)
        {
            case <= 0x07 when ExternalRam.NumberOfBanks > 0:
                // RAM Bank write
                ExternalRam.CurrentBank = _ramBankOrRtcSelect % ExternalRam.NumberOfBanks;
                ExternalRam.WriteByte(ref address, ref value);
                break;
            case 0x08:
                _rtcSeconds = (byte)(value & 0x3F); // 0-59
                break;
            case 0x09:
                _rtcMinutes = (byte)(value & 0x3F); // 0-59
                break;
            case 0x0A:
                _rtcHours = (byte)(value & 0x1F); // 0-23
                break;
            case 0x0B:
                _rtcDaysLow = value;
                break;
            case 0x0C:
                _rtcDaysHigh = (byte)(value & 0xC1); // Bits 0, 6, 7 only
                break;
        }
    }

    public override byte ReadByte(ref ushort address)
    {
        return address switch
        {
            // 0000-3FFF: ROM Bank 00
            <= BankAddress.RomBank0End 
                => MemorySpace[address - StartAddress],
            // A000-BFFF: RAM Bank or RTC Register
            >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd 
                => ReadRamOrRtc(ref address),
            // 4000-7FFF: Switchable ROM Bank
            _ => MemorySpace[(CurrentBank % NumberOfBanks) * BankSizeInBytes + (address - BankAddress.RomBankNnStart)]
        };
    }
    
    private byte ReadRamOrRtc(ref ushort address)
    {
        if (!ExternalRamEnabled)
            return 0xFF;
            
        return _ramBankOrRtcSelect switch
        {
            // RAM Banks
            <= 0x07 when ExternalRam.NumberOfBanks > 0 => ReadFromRamBank(ref address),
            // RTC Registers (return latched values)
            0x08 => _rtcSecondsLatched,
            0x09 => _rtcMinutesLatched,
            0x0A => _rtcHoursLatched,
            0x0B => _rtcDaysLowLatched,
            0x0C => _rtcDaysHighLatched,
            _ => 0xFF
        };
    }
    
    private byte ReadFromRamBank(ref ushort address)
    {
        ExternalRam.CurrentBank = _ramBankOrRtcSelect % ExternalRam.NumberOfBanks;
        return ExternalRam.ReadByte(ref address);
    }
    
    private void UpdateRomBank()
    {
        // ROM Bank Number: 0x00 treated as 0x01
        CurrentBank = _romBankNumber == 0 ? 1 : _romBankNumber;
        CurrentBank %= NumberOfBanks;
    }
    
    private void LatchRtcRegisters()
    {
        // Copy internal (ticking) RTC registers to latched (readable) registers
        _rtcSecondsLatched = _rtcSeconds;
        _rtcMinutesLatched = _rtcMinutes;
        _rtcHoursLatched = _rtcHours;
        _rtcDaysLowLatched = _rtcDaysLow;
        _rtcDaysHighLatched = _rtcDaysHigh;
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