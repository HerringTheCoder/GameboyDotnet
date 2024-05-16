using GameboyDotnet.Extensions;
using GameboyDotnet.Memory.BuildingBlocks;

namespace GameboyDotnet.Memory.Mbc;

public class Mbc3(string name, int bankSizeInBytes, int numberOfBanks)
    : MemoryBankController(name, bankSizeInBytes, numberOfBanks)
{
    private byte RtcSeconds; // 0x08, RTC S register (0-59)
    private byte RtcMinutes; // 0x09, RTC M register (0-59)
    private byte RtcHours; // 0x0A, RTC H register (0-23)
    private byte RtcDaysLow; // 0x0B, RTC DL register (0-255)
    private byte RtcDaysHigh; // 0x0C, RTC DH register (0-1)

    public override void WriteByte(ref ushort address, ref byte value)
    {
        if (ExternalRamEnabled && address is >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd)
        {
            switch (ExternalRam.CurrentBank)
            {
                case 0x00 or 0x01 or 0x02 or 0x03:
                    ExternalRam.WriteByte(ref address, ref value);
                    break;
                case 0x08:
                    RtcSeconds = value;
                    break;
                case 0x09:
                    RtcMinutes = value;
                    break;
                case 0x0A:
                    RtcHours = value;
                    break;
                case 0x0B:
                    RtcDaysLow = value;
                    break;
                case 0x0C:
                    RtcDaysHigh = value;
                    break;
            }

            return;
        }

        switch (address)
        {
            case <= 0x1FFF:
                ExternalRamEnabled = (value & 0x0A) == 0x0A;
                break;
            case <= 0x3FFF:
                CurrentBank = value & 0x7F;
                if (CurrentBank == 0)
                    CurrentBank = 1;
                break;
            case <= 0x5FFF:
                if (value is <= 0x03 or >= 0x08 and <= 0x0C)
                    ExternalRam.CurrentBank = value;
                break;
            case <= 0x7FFF:
                var currentTime = DateTime.Now; //TODO: Timezone config?
                RtcSeconds = (byte)currentTime.Second;
                RtcMinutes = (byte)currentTime.Minute;
                RtcHours = (byte)currentTime.Hour;
                break;
        }
        
        //Only External Ram writes are allowed
        if (address is < BankAddress.ExternalRamStart or > BankAddress.ExternalRamEnd) 
            return;
        
        if (!ExternalRamEnabled)
            return;
            
        ExternalRam.WriteByte(ref address, ref value);
    }

    public override byte ReadByte(ref ushort address)
    {
        switch (address)
        {
            case <= BankAddress.RomBank0End:
                return MemorySpace[address];
            case >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd when !ExternalRamEnabled:
                return 0xFF;
            case >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd:
                return ExternalRam.CurrentBank switch
                {
                    0x00 or 0x01 or 0x02 or 0x03 => ExternalRam.ReadByte(ref address),
                    0x08 => RtcSeconds,
                    0x09 => RtcMinutes,
                    0x0A => RtcHours,
                    0x0B => RtcDaysLow,
                    0x0C => RtcDaysHigh,
                    _ => 0xFF
                };
            default:
                return MemorySpace[CurrentBank * BankSizeInBytes + address - BankAddress.RomBankNnStart];
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