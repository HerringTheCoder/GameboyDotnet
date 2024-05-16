using GameboyDotnet.Extensions;
using GameboyDotnet.Memory.BuildingBlocks;

namespace GameboyDotnet.Memory.Mbc;

//https://gbdev.io/pandocs/MBC1.html

public class Mbc1( string name, int bankSizeInBytes, int numberOfBanks) 
    : MemoryBankController(name, bankSizeInBytes, numberOfBanks)
{
    public override void WriteByte(ref ushort address, ref byte value)
    {
        switch (address)
        {
            case <= 0x1FFF:
                ExternalRamEnabled = (value & 0x0A) == 0x0A;
                break;
            case <= 0x3FFF:
            {
                CurrentBank = value & 0x1F;
                if(CurrentBank is 0x00 or 0x20 or 0x40 or 0x60)
                    CurrentBank++;
                break;
            }
            case <= 0x5FFF when RomBankingMode:
            {
                CurrentBank = (CurrentBank & 0x1F) | ((value & 0x03) << 5);
                if(CurrentBank is 0x00 or 0x20 or 0x40 or 0x60)
                    CurrentBank++;
                break;
            }
            case < 0x5FFF:
                ExternalRam.CurrentBank = value & 0x03;
                break;
            default:
                RomBankingMode = (value & 0x01) == 0x01;
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
        return address switch
        {
            <= BankAddress.RomBank0End => MemorySpace[address - StartAddress],
            >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd 
                => ExternalRamEnabled ? ExternalRam.ReadByte(ref address) : (byte)0xFF,
            _ => MemorySpace[CurrentBank * BankSizeInBytes + address - BankAddress.RomBankNnStart]
        };
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